// Scripts/BasketballManager.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Managers
{
    public class BasketballManager : MonoBehaviour
    {
        public static BasketballManager Instance { get; private set; }

        [Header("Game Elements")] [SerializeField]
        private GameObject basketballPrefab;

        [SerializeField] private Transform shootingOrigin; // Assign your main camera transform here

        [Header("Shooting Settings")] [SerializeField]
        private float minShootForce = 500f;

        [SerializeField] private float maxShootForce = 1200f;
        [SerializeField] private float chargeTime = 1.5f;

        [Header("Mobile Settings")] [SerializeField]
        private float mobileChargeSpeed = 2f; // How fast the charge builds up on mobile

        private GameObject _currentBall;
        private Rigidbody _currentBallRb;
        private InputManager _inputManager;
        private UIManager _uiManager;
        private GameManager _gameManager;
        private InteractionManager _interactionManager;

        private bool _isCharging;
        private float _chargeStartTime;
        private float _currentCharge = 0f; // For mobile tap-based charging

        // Track all spawned basketballs for cleanup
        private readonly List<GameObject> _spawnedBasketballs = new List<GameObject>();

        public bool IsInBasketballMode()
        {
            return _gameManager != null && _gameManager.CurrentMode == GameManager.ControlMode.Basketball;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Safely get instances of other managers
            _inputManager = InputManager.Instance;
            _uiManager = UIManager.Instance;
            _gameManager = GameManager.Instance;
            _interactionManager = FindObjectOfType<InteractionManager>();
        }

        public void StartShootingMode()
        {
            if (IsInBasketballMode()) return;

            _gameManager.SetMode(GameManager.ControlMode.Basketball);

            // Set platform-specific hints
            if (GameManager.IsMobilePlatform)
            {
                _uiManager.SetHint("Tap Throw to charge up shot. Press Exit to stop playing.");
            }
            else
            {
                _uiManager.SetHint("Hold Click to charge shot. Press E to exit.");
            }

            SpawnBall();

            _uiManager.SetBasketballMode(true);

            // Listen for "Attack" (LMB/Pickup Button) to charge and shoot - Desktop only
            if (!GameManager.IsMobilePlatform)
            {
                _inputManager.PlayerControls.Player.Attack.started += OnShootStarted;
                _inputManager.PlayerControls.Player.Attack.canceled += OnShootCanceled;
            }

            // Reset mobile charge
            _currentCharge = 0f;
        }

        public void ExitShootingMode()
        {
            if (!IsInBasketballMode()) return;

            // Stop listening to inputs to prevent errors
            if (!GameManager.IsMobilePlatform)
            {
                _inputManager.PlayerControls.Player.Attack.started -= OnShootStarted;
                _inputManager.PlayerControls.Player.Attack.canceled -= OnShootCanceled;
            }
            
            // Cancel any pending ball spawns to prevent floating basketballs
            CancelInvoke(nameof(SpawnBall));

            // Clean up ALL basketballs, not just the current one
            CleanupAllBasketballs();

            _uiManager.ClearHint();
            _gameManager.SetMode(GameManager.ControlMode.Camera);
            _uiManager.SetBasketballMode(false);

            // Re-enable the main interaction manager
            if (_interactionManager) _interactionManager.enabled = true;

            // Reset mobile charge
            _currentCharge = 0f;
        }

        private void CleanupAllBasketballs()
        {
            // Destroy the current ball if it exists
            if (_currentBall)
            {
                Destroy(_currentBall);
                _currentBall = null;
                _currentBallRb = null;
            }

            // Clean up the list and destroy any remaining basketballs
            for (int i = _spawnedBasketballs.Count - 1; i >= 0; i--)
            {
                if (_spawnedBasketballs[i] != null)
                {
                    Destroy(_spawnedBasketballs[i]);
                }
            }

            _spawnedBasketballs.Clear();

            // Also find any remaining basketballs by tag and destroy them
            GameObject[] remainingBalls = GameObject.FindGameObjectsWithTag("Basketball");
            foreach (GameObject ball in remainingBalls)
            {
                Destroy(ball);
            }
        }

        private void SpawnBall()
        {
            if (_currentBall) Destroy(_currentBall);
            if (shootingOrigin == null) return;

            Vector3 spawnPos = shootingOrigin.position + shootingOrigin.forward * 1.5f;
            _currentBall = Instantiate(basketballPrefab, spawnPos, Quaternion.identity);
            _currentBallRb = _currentBall.GetComponent<Rigidbody>();
            _currentBallRb.isKinematic = true;

            // Add to our tracking list
            _spawnedBasketballs.Add(_currentBall);
        }

        // Desktop controls - hold to charge
        private void OnShootStarted(InputAction.CallbackContext context)
        {
            _isCharging = true;
            _chargeStartTime = Time.time;
        }

        private void OnShootCanceled(InputAction.CallbackContext context)
        {
            if (!_isCharging) return;
            _isCharging = false;

            float chargeDuration = Time.time - _chargeStartTime;
            float chargeRatio = Mathf.Clamp01(chargeDuration / chargeTime);
            float shootForce = Mathf.Lerp(minShootForce, maxShootForce, chargeRatio);

            Shoot(shootForce);
        }

        private void Shoot(float force)
        {
            if (!_currentBallRb) return;

            _currentBallRb.isKinematic = false;
            _currentBallRb.AddForce(shootingOrigin.forward * force);

            // Clear the current ball reference since it's now physics-based
            _currentBall = null;
            _currentBallRb = null;

            // Respawn a new ball after a short delay
            Invoke(nameof(SpawnBall), 2.5f);
        }

        // Mobile controls - tap to charge, automatic shoot when fully charged
        public void ShootWithFixedForce()
        {
            if (!IsInBasketballMode()) return;

            if (GameManager.IsMobilePlatform)
            {
                // Mobile: Each tap increases charge
                _currentCharge += mobileChargeSpeed * Time.deltaTime * 10f; // Multiply to make taps more significant
                _currentCharge = Mathf.Clamp01(_currentCharge);

                // Update UI to show charge level
                if (_currentCharge < 1f)
                {
                    int chargePercent = Mathf.RoundToInt(_currentCharge * 100);
                    _uiManager.SetHint($"Charge: {chargePercent}% - Tap to charge more!");
                }
                else
                {
                    // Automatically shoot when fully charged
                    float shootForce = Mathf.Lerp(minShootForce, maxShootForce, _currentCharge);
                    Shoot(shootForce);
                    _currentCharge = 0f; // Reset charge
                    _uiManager.SetHint("Shot fired! Tap Throw to charge up next shot.");
                }
            }
            else
            {
                // Desktop fallback (shouldn't normally be called)
                float shootForce = Mathf.Lerp(minShootForce, maxShootForce, 0.5f);
                Shoot(shootForce);
            }
        }

        public void OnScore()
        {
            if (!IsInBasketballMode()) return;

            _uiManager.SetHint("Nice Shot!");
            Invoke(nameof(RestoreDefaultHint), 2f);
        }

        private void Update()
        {
            if (IsInBasketballMode() && _currentBall != null && _currentBallRb.isKinematic)
            {
                _currentBall.transform.position = shootingOrigin.position + shootingOrigin.forward * 1.5f;
                _currentBall.transform.rotation = shootingOrigin.rotation;

                // Visual feedback for mobile charging
                if (GameManager.IsMobilePlatform && _currentCharge > 0f)
                {
                    // Optional: Add visual feedback like scaling the ball based on charge
                    float scale = 1f + (_currentCharge * 0.2f); // Scale from 1 to 1.2
                    _currentBall.transform.localScale = Vector3.one * scale;
                }
            }

            // Clean up destroyed basketballs from our tracking list
            for (int i = _spawnedBasketballs.Count - 1; i >= 0; i--)
            {
                if (_spawnedBasketballs[i] == null)
                {
                    _spawnedBasketballs.RemoveAt(i);
                }
            }

            // Desktop charging visual feedback
            if (!GameManager.IsMobilePlatform && _isCharging && _currentBall != null)
            {
                float chargeDuration = Time.time - _chargeStartTime;
                float chargeRatio = Mathf.Clamp01(chargeDuration / chargeTime);
                float scale = 1f + (chargeRatio * 0.2f); // Scale from 1 to 1.2
                _currentBall.transform.localScale = Vector3.one * scale;
            }
        }

        private void RestoreDefaultHint()
        {
            if (IsInBasketballMode())
            {
                if (GameManager.IsMobilePlatform)
                {
                    _uiManager.SetHint("Tap Throw to charge up shot. Press Exit to stop playing.");
                }
                else
                {
                    _uiManager.SetHint("Hold Click to charge shot. Press E to exit.");
                }
            }
        }
    }
}