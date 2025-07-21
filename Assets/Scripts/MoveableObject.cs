using Interfaces;
using Managers;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class MoveableObject : MonoBehaviour, IResetable, IHasName
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSmoothTime = 0.05f;
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float moveStepAmount = 0.1f;
    [SerializeField] private float rotationStepAmount = 15f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioClip dropSound;

    public bool IsNewlySpawned { get; private set; }

    private Rigidbody _rigidbody;
    private Collider _collider;
    private Camera _mainCamera;
    
    private bool _isHeld;
    private Vector3 _targetPosition;
    private Vector3 _velocity = Vector3.zero;
    private float _heldDistance;
    private Vector3 _scrollRotationAxis = Vector3.up;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    // --- Resilient Manager Properties ---
    private UIManager _uiManager;
    private UIManager UIManager => _uiManager ??= UIManager.Instance;

    private AudioManager _audioManager;
    private AudioManager AudioManager => _audioManager ??= AudioManager.Instance;

    private GameManager _gameManager;
    private GameManager GameManager => _gameManager ??= GameManager.Instance;

    private InputManager _inputManager;
    private InputManager InputManager => _inputManager ??= InputManager.Instance;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>(); 
        _mainCamera = Camera.main;
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
    }

    private void Update()
    {
        if (!_isHeld) return;
        
        var ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        _targetPosition = ray.GetPoint(_heldDistance);

        if (!GameManager.IsMobilePlatform)
        {
            HandleDesktopInput();
        }
    }

    private void FixedUpdate()
    {
        if (!_isHeld) return;

        var newPosition = Vector3.SmoothDamp(_rigidbody.position, _targetPosition, ref _velocity, moveSmoothTime);
        var direction = newPosition - _rigidbody.position;
        var distance = direction.magnitude;

        if (!Physics.BoxCast(_rigidbody.position, _collider.bounds.extents, direction.normalized, out RaycastHit hit, transform.rotation, distance))
        {
            _rigidbody.MovePosition(newPosition);
        }
    }

    public void ApplyRotationStep(float direction)
    {
        transform.Rotate(Vector3.right, direction * rotationStepAmount, Space.World);
    }

    public void AdjustDistanceStep(float direction)
    {
        if (!_isHeld) return;
        _heldDistance += direction * moveStepAmount;
        _heldDistance = Mathf.Clamp(_heldDistance, 1f, 10f);
    }

    public void ApplyHorizontalMovementStep(float direction)
    {
        if (!_isHeld) return;
        Vector3 right = _mainCamera.transform.right;
        right.y = 0;
        _targetPosition += right.normalized * direction * moveStepAmount;
    }

    public void Pickup(bool isNewlySpawned = false)
    {
        if (GameManager.CurrentMode != GameManager.ControlMode.Camera && !isNewlySpawned) return;
        if (_isHeld) return;
        _isHeld = true;
        
        IsNewlySpawned = isNewlySpawned;

        if (IsNewlySpawned)
        {
            _heldDistance = 5f; 
        }
        else
        {
            _heldDistance = Vector3.Distance(_mainCamera.transform.position, transform.position);
        }

        GameManager.CurrentHeldObject = this;
        GameManager.SetMode(GameManager.ControlMode.ObjectHolding);
        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = true;
        _targetPosition = transform.position;
        _velocity = Vector3.zero;
        AudioManager.PlaySound(audioSource, pickupSound);
    }

    public void Drop()
    {
        if (!_isHeld) return;
        _isHeld = false;
        IsNewlySpawned = false; 
        
        GameManager.CurrentHeldObject = null;
        GameManager.SetMode(GameManager.ControlMode.Camera);
        _rigidbody.useGravity = true;
        _rigidbody.isKinematic = false;
        AudioManager.PlaySound(audioSource, dropSound);
    }

    private void HandleDesktopInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) _scrollRotationAxis = Vector3.right;
        if (Input.GetKeyDown(KeyCode.Alpha2)) _scrollRotationAxis = Vector3.up;
        if (Input.GetKeyDown(KeyCode.Alpha3)) _scrollRotationAxis = Vector3.forward;

        var scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            transform.Rotate(_scrollRotationAxis, scrollInput * rotationSpeed * 10f, Space.Self);
        }

        if (InputManager.PlayerControls.Player.Comma.IsPressed()) AdjustDistanceStep(-1);
        if (InputManager.PlayerControls.Player.Dot.IsPressed()) AdjustDistanceStep(1);
    }

    public void ResetObject()
    {
        if (_isHeld) Drop();
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;
    }
    
    public bool IsHeld => _isHeld;

    [SerializeField] private new string name;

    public string Name => name;
}