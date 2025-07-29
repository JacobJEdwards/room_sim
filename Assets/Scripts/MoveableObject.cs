// Scripts/MoveableObject.cs
using System.Collections;
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

    [Header("Collision & Phasing")]
    [Tooltip("How long to push against an object before the held object phases out.")]
    [SerializeField] private float phaseDelay = 0.75f;
    [Tooltip("How long the held object's collider is disabled during a phase.")]
    [SerializeField] private float phaseDuration = 1.5f;

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

    // Phasing state
    private float _phaseTimer;
    private Collider _lastPhaseCandidate;
    private bool _isPhasing;

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

        // Temporarily enable the collider for the collision check, but only if we are not currently phasing.
        if (!_isPhasing)
        {
            _collider.enabled = true;
        }

        bool canMove = true;
        // The BoxCast now uses the held object's own (temporarily enabled) collider to check for hits.
        if (distance > 0.001f && Physics.BoxCast(_rigidbody.position, _collider.bounds.extents, direction.normalized, out RaycastHit hit, transform.rotation, distance))
        {
            // If we hit another moveable object or a wall, stop moving.
            if (hit.collider.GetComponent<MoveableObject>() != null || hit.collider.CompareTag("wall"))
            {
                canMove = false;
                ResetPhaseTimer();
            }
            // If we hit something else, start the timer to phase the held object.
            else
            {
                UpdatePhaseTimer();
                if (_phaseTimer < phaseDelay)
                {
                    canMove = false;
                }
            }
        }
        else
        {
            ResetPhaseTimer();
        }

        // Disable the collider again after the check to allow free movement.
        if (!_isPhasing)
        {
            _collider.enabled = false;
        }
        
        if (canMove)
        {
            _rigidbody.MovePosition(newPosition);
        }
    }

    private void UpdatePhaseTimer()
    {
        _phaseTimer += Time.deltaTime;

        // If the timer is up, start the phasing coroutine.
        if (_phaseTimer >= phaseDelay)
        {
            if (!_isPhasing) // Ensure coroutine is not already running
            {
                 StartCoroutine(TemporarilyDisableHeldObjectCollider());
            }
            ResetPhaseTimer();
        }
    }

    private void ResetPhaseTimer()
    {
        _phaseTimer = 0f;
    }

    // This coroutine now disables THIS object's collider.
    private IEnumerator TemporarilyDisableHeldObjectCollider()
    {
        _isPhasing = true;
        _collider.enabled = false; // Disable our own collider

        yield return new WaitForSeconds(phaseDuration);

        _collider.enabled = true; // Re-enable our own collider
        _isPhasing = false;
    }


    public void Pickup(bool isNewlySpawned = false)
    {
        if (GameManager.CurrentMode != GameManager.ControlMode.Camera && !isNewlySpawned) return;
        if (_isHeld) return;
        _isHeld = true;

        // Collider is enabled here to allow the initial BoxCast to work. It's disabled in FixedUpdate.
        _collider.enabled = true;

        IsNewlySpawned = isNewlySpawned;

        if (IsNewlySpawned)
        {
            _heldDistance = 2f;
        }
        else
        {
            float distance = Vector3.Distance(_mainCamera.transform.position, transform.position);
            _heldDistance = Mathf.Clamp(distance, 1.5f, 4f);
        }

        GameManager.CurrentHeldObject = this;
        GameManager.SetMode(GameManager.ControlMode.ObjectHolding);
        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = true;
        _targetPosition = transform.position;
        _velocity = Vector3.zero;
        AudioManager.PlaySound(audioSource, pickupSound);
    }

    public void Drop(bool preventModeChange = false)
    {
        if (!_isHeld) return;
        _isHeld = false;

        // Always ensure the collider is enabled when dropped.
        _collider.enabled = true;
        _isPhasing = false; // Stop any phasing
        StopAllCoroutines(); // Stop the phasing coroutine if it's running

        IsNewlySpawned = false;
        ResetPhaseTimer();

        GameManager.CurrentHeldObject = null;
        if (!preventModeChange)
        {
            GameManager.SetMode(GameManager.ControlMode.Camera);
        }
        _rigidbody.useGravity = true;
        _rigidbody.isKinematic = false;
        AudioManager.PlaySound(audioSource, dropSound);
    }

    #region Unchanged Code
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
    #endregion
}