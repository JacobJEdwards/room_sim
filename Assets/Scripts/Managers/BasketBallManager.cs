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

        [SerializeField] private Transform shootingOrigin; 

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
            _inputManager = InputManager.Instance;
            _uiManager = UIManager.Instance;
            _gameManager = GameManager.Instance;
            _interactionManager = FindObjectOfType<InteractionManager>();
        }

        public void StartShootingMode()
        {
            if (IsInBasketballMode()) return;

            _gameManager.SetMode(GameManager.ControlMode.Basketball);

            if (GameManager.IsMobilePlatform)
            {
                _uiManager.SetHint("Tap Throw to shoot. Press Exit to stop playing.");
            }
            else
            {
                _uiManager.SetHint("Hold Click to shoot. Press E to exit.");
            }

            SpawnBall();

            _uiManager.SetBasketballMode(true);

            if (!GameManager.IsMobilePlatform)
            {
                _inputManager.PlayerControls.Player.Attack.started += OnShootStarted;
                _inputManager.PlayerControls.Player.Attack.canceled += OnShootCanceled;
            }
        }

        public void ExitShootingMode()
        {
            if (!IsInBasketballMode()) return;

            if (!GameManager.IsMobilePlatform)
            {
                _inputManager.PlayerControls.Player.Attack.started -= OnShootStarted;
                _inputManager.PlayerControls.Player.Attack.canceled -= OnShootCanceled;
            }

            CancelInvoke(nameof(SpawnBall));

            CleanupAllBasketballs();

            _uiManager.ClearHint();
            _gameManager.SetMode(GameManager.ControlMode.Camera);
            _uiManager.SetBasketballMode(false);

            if (_interactionManager) _interactionManager.enabled = true;
        }

        private void CleanupAllBasketballs()
        {
            if (_currentBall)
            {
                Destroy(_currentBall);
                _currentBall = null;
                _currentBallRb = null;
            }

            for (int i = _spawnedBasketballs.Count - 1; i >= 0; i--)
            {
                if (_spawnedBasketballs[i] != null)
                {
                    Destroy(_spawnedBasketballs[i]);
                }
            }

            _spawnedBasketballs.Clear();

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
            _currentBallRb.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic; 

            _spawnedBasketballs.Add(_currentBall);
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

            _currentBall = null;
            _currentBallRb = null;

            Invoke(nameof(SpawnBall), 2.5f);
        }

        public void ShootWithFixedForce()
        {
            if (!IsInBasketballMode()) return;

            if (GameManager.IsMobilePlatform)
            {
                float shootForce = Mathf.Lerp(minShootForce, maxShootForce, 0.5f);
                Shoot(shootForce);
            }
            else
            {
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
            }

            for (int i = _spawnedBasketballs.Count - 1; i >= 0; i--)
            {
                if (_spawnedBasketballs[i] == null)
                {
                    _spawnedBasketballs.RemoveAt(i);
                }
            }

            if (!GameManager.IsMobilePlatform && _isCharging && _currentBall != null)
            {
                float chargeDuration = Time.time - _chargeStartTime;
                float chargeRatio = Mathf.Clamp01(chargeDuration / chargeTime);
                float scale = 1f + (chargeRatio * 0.2f); 
                _currentBall.transform.localScale = Vector3.one * scale;
            }
        }

        private void RestoreDefaultHint()
        {
            if (IsInBasketballMode())
            {
                if (GameManager.IsMobilePlatform)
                {
                    _uiManager.SetHint("Tap Throw to shoot. Press Exit to stop playing.");
                }
                else
                {
                    _uiManager.SetHint("Hold Click to shoot. E to exit.");
                }
            }
        }
    }
}