// Scripts/MoveableObject.cs

using System.Collections;
using System.Linq;
using Interfaces;
using Managers;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class MoveableObject : MonoBehaviour, IResetable, IHasName
{
    [Header("Movement Settings")] [SerializeField]
    private float moveSmoothTime = 0.05f;

    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float moveStepAmount = 0.1f;
    [SerializeField] private float rotationStepAmount = 15f;
    [SerializeField] private bool useGravity = true;

    [Header("Force Settings")]
    [Tooltip("Whether this object can apply/receive forces from collisions")]
    [SerializeField]
    private bool useForce = true;

    [Header("Collision & Phasing")]
    [Tooltip("Force applied to other moveable objects when pushing them.")]
    [SerializeField]
    private float pushForce = 1.2f;

    [Tooltip("Maximum velocity for pushed objects.")] [SerializeField]
    private float maxPushVelocity = 2f;

    [Tooltip("How long to push against a non-moveable object before phasing through.")]
    private float phaseDelay = 0.5f;

    [Tooltip("How long the held object's collider is disabled during a phase.")]
    private float phaseDuration = 0.5f;

    [Header("Audio")] [SerializeField] private AudioSource audioSource;
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
    private bool _isPhasing;
    private Collider _currentPhaseCandidate;

    // --- Resilient Manager Properties ---
    private UIManager _uiManager;
    private UIManager UIManager => _uiManager ??= UIManager.Instance;
    private AudioManager _audioManager;
    private AudioManager AudioManager => _audioManager ??= AudioManager.Instance;
    private GameManager _gameManager;
    private GameManager GameManager => _gameManager ??= GameManager.Instance;
    private InputManager _inputManager;
    private InputManager InputManager => _inputManager ??= InputManager.Instance;

    // Public property to check if this object can be pushed by others
    public bool CanBePushed => useForce;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _mainCamera = Camera.main;
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        // If this object can't be pushed, make it kinematic by default
        if (!useForce)
        {
            _rigidbody.isKinematic = true;
        }
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

        if (!_isPhasing)
        {
            _collider.enabled = true;
        }

        bool canMove = true;

        if (distance > 0.001f)
        {
            RaycastHit[] hits = Physics.BoxCastAll(_rigidbody.position, _collider.bounds.extents, direction.normalized,
                transform.rotation, distance);

            if (hits.Length > 0)
            {
                bool isHittingWall = hits.Any(h => h.collider.CompareTag("wall"));
                bool isHittingFloor = hits.Any(h => h.collider.CompareTag("Floor"));
                bool isHittingMoveable = hits.Any(h => h.collider.GetComponent<MoveableObject>() != null);

                if (isHittingWall)
                {
                    // Hitting a wall - stop completely, no phasing through walls
                    canMove = false;
                    ResetPhaseTimer();
                    _currentPhaseCandidate = hits.First(h => h.collider.CompareTag("wall")).collider;
                }
                else if (isHittingFloor)
                {
                    // Hitting floor - allow horizontal movement, but prevent sinking into floor
                    Vector3 horizontalDirection = new Vector3(direction.x, 0, direction.z);

                    // Only allow movement if it's mostly horizontal, or if moving upward
                    if (direction.y >= -0.1f || horizontalDirection.magnitude > Mathf.Abs(direction.y))
                    {
                        canMove = true;

                        // If trying to move down into floor, restrict to horizontal movement only
                        if (direction.y < -0.1f)
                        {
                            newPosition = new Vector3(newPosition.x, _rigidbody.position.y, newPosition.z);
                        }
                    }
                    else
                    {
                        canMove = false;
                    }

                    ResetPhaseTimer();
                }
                else if (isHittingMoveable)
                {
                    // Hitting another moveable object - allow physics to push it
                    canMove = true;
                    ResetPhaseTimer();
                }
                else
                {
                    // Hitting other objects (furniture, etc.) - use phasing system
                    _currentPhaseCandidate = hits[0].collider;
                    UpdatePhaseTimer();

                    if (_phaseTimer < phaseDelay)
                    {
                        canMove = false;
                    }
                    else if (!_isPhasing)
                    {
                        StartCoroutine(TemporarilyDisableCollider());
                    }
                }
            }
            else
            {
                // No obstacles
                ResetPhaseTimer();
                _currentPhaseCandidate = null;
            }
        }

        if (canMove)
        {
            _rigidbody.MovePosition(newPosition);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Prevent any object from pushing objects that can't be pushed
        var otherMoveable = collision.gameObject.GetComponent<MoveableObject>();
        if (otherMoveable != null && !otherMoveable.CanBePushed)
        {
            // Cancel out any velocity that would push the unpushable object
            if (_rigidbody && !_isHeld)
            {
                var relativeVelocity = _rigidbody.linearVelocity -
                                       (collision.rigidbody ? collision.rigidbody.linearVelocity : Vector3.zero);
                var normalVelocity = Vector3.Dot(relativeVelocity, collision.contacts[0].normal);
                if (normalVelocity < 0)
                {
                    _rigidbody.linearVelocity += collision.contacts[0].normal * normalVelocity;
                }
            }

            return;
        }

        if (useForce)
        {
            HandleCollisionWithMoveableObject(collision);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (useForce)
        {
            HandleCollisionWithMoveableObject(collision);
        }
    }

    private void HandleCollisionWithMoveableObject(Collision collision)
    {
        if (!_isHeld) return;

        var otherMoveable = collision.gameObject.GetComponent<MoveableObject>();
        if (otherMoveable == null || otherMoveable._isHeld) return;

        // Check if the other object can be pushed
        if (!otherMoveable.CanBePushed) return;

        Vector3 pushDirection = Vector3.zero;
        foreach (ContactPoint contact in collision.contacts)
        {
            pushDirection += contact.normal;
        }

        pushDirection = -pushDirection.normalized;
        pushDirection.y = 0;

        Rigidbody otherRb = collision.rigidbody;
        if (otherRb != null && !otherRb.isKinematic)
        {
            float forceMagnitude = pushForce;
            otherRb.AddForce(pushDirection * forceMagnitude, ForceMode.VelocityChange);

            if (otherRb.linearVelocity.magnitude > maxPushVelocity)
            {
                otherRb.linearVelocity = otherRb.linearVelocity.normalized * maxPushVelocity;
            }

            otherRb.AddForce(Vector3.up * 0.05f, ForceMode.VelocityChange);
        }
    }

    private void UpdatePhaseTimer()
    {
        _phaseTimer += Time.deltaTime;
    }

    private void ResetPhaseTimer()
    {
        _phaseTimer = 0f;
    }

    private IEnumerator TemporarilyDisableCollider()
    {
        _isPhasing = true;
        _collider.enabled = false;

        yield return new WaitForSeconds(phaseDuration);

        _collider.enabled = true;
        _isPhasing = false;
        ResetPhaseTimer();
    }

    public void Pickup(bool isNewlySpawned = false)
    {
        if (GameManager.CurrentMode != GameManager.ControlMode.Camera && !isNewlySpawned) return;
        if (_isHeld) return;
        _isHeld = true;

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
        _rigidbody.isKinematic = false; // Always make non-kinematic when held
        _rigidbody.linearDamping = 8f;
        _rigidbody.angularDamping = 8f;
        _targetPosition = transform.position;
        _velocity = Vector3.zero;
        AudioManager.PlaySound(audioSource, pickupSound);
    }

    public void Drop(bool preventModeChange = false)
    {
        if (!_isHeld) return;
        _isHeld = false;

        _collider.enabled = true;
        _isPhasing = false;
        StopAllCoroutines();

        IsNewlySpawned = false;
        ResetPhaseTimer();
        _currentPhaseCandidate = null;

        GameManager.CurrentHeldObject = null;
        if (!preventModeChange)
        {
            GameManager.SetMode(GameManager.ControlMode.Camera);
        }

        _rigidbody.useGravity = useGravity;
        // If this object can't be pushed, make it kinematic when dropped
        _rigidbody.isKinematic = !useForce;
        _rigidbody.linearDamping = 0f;
        _rigidbody.angularDamping = 0.05f;
        AudioManager.PlaySound(audioSource, dropSound);
    }


    public void ApplyRotationStep(float direction)
    {
        transform.Rotate(Vector3.right, direction * rotationStepAmount, Space.World);
    }

    // --- MODIFIED ---
    public void AdjustDistanceStep(float direction)
    {
        if (!_isHeld) return;
        // Move the object along the camera's forward vector
        Vector3 forward = _mainCamera.transform.forward;
        transform.position += forward * direction * moveStepAmount;
        // Update the target position and held distance to prevent snapping back
        _targetPosition = transform.position;
        _heldDistance = Vector3.Distance(_mainCamera.transform.position, transform.position);
    }

    // --- MODIFIED ---
    public void ApplyHorizontalMovementStep(float direction)
    {
        if (!_isHeld) return;
        // Move the object along the camera's right vector
        Vector3 right = _mainCamera.transform.right;
        transform.position += right * direction * moveStepAmount;
        // Update the target position and held distance to prevent snapping back
        _targetPosition = transform.position;
        _heldDistance = Vector3.Distance(_mainCamera.transform.position, transform.position);
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