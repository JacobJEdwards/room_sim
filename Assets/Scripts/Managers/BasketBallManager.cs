// Scripts/BasketballManager.cs

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

        private GameObject _currentBall;
        private Rigidbody _currentBallRb;
        private InputManager _inputManager;
        private UIManager _uiManager;
        private GameManager _gameManager;
        private InteractionManager _interactionManager;

        private bool _isCharging;
        private float _chargeStartTime;

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
            _uiManager.SetHint("Hold Click/Pickup to charge. Press E to exit.");

            SpawnBall();
            
            _uiManager.SetBasketballMode(true);

            // Listen for "Attack" (LMB/Pickup Button) to charge and shoot
            if (!GameManager.IsMobilePlatform)
            {
                _inputManager.PlayerControls.Player.Attack.started += OnShootStarted;
                _inputManager.PlayerControls.Player.Attack.canceled += OnShootCanceled;
            }
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

            if (_currentBall) Destroy(_currentBall);

            _uiManager.ClearHint();
            _gameManager.SetMode(GameManager.ControlMode.Camera);
            _uiManager.SetBasketballMode(false);


            // Re-enable the main interaction manager
            if (_interactionManager) _interactionManager.enabled = true;
        }

        private void SpawnBall()
        {
            if (_currentBall) Destroy(_currentBall);
            if (shootingOrigin == null) return;

            Vector3 spawnPos = shootingOrigin.position + shootingOrigin.forward * 1.5f;
            _currentBall = Instantiate(basketballPrefab, spawnPos, Quaternion.identity);
            _currentBallRb = _currentBall.GetComponent<Rigidbody>();
            _currentBallRb.isKinematic = true;
        }

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

            // Respawn a new ball after a short delay
            Invoke(nameof(SpawnBall), 2.5f);
        }
        
        public void ShootWithFixedForce()
        {
            if (!IsInBasketballMode()) return;
            // Use a medium force for a simple tap
            float shootForce = Mathf.Lerp(minShootForce, maxShootForce, 0.5f);
            Shoot(shootForce);
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
            }
        }

        private void RestoreDefaultHint()
        {
            if (IsInBasketballMode())
            {
                _uiManager.SetHint("Hold Click/Pickup to charge. Press E to exit.");
            }
        }
    }
}