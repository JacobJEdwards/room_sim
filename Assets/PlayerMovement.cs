using Managers;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input Settings")]
    private InputSystem _inputActions;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    [SerializeField] private Transform head;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float sprintSpeed = 8.0f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 5.0f;
    [SerializeField] private float gravityMultiplier = 2.0f;
    private float _gravity;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckTransform;
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private float lookSensitivity = 0.5f;

    private CharacterController _characterController;
    private InputManager _inputManager = null!;
    private Vector2 _moveInput;
    private bool _jumpRequested;

    private float _currentRotationX;
    private Vector3 _playerVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _gravity = Physics.gravity.y * gravityMultiplier;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _inputManager = InputManager.Instance;
        if (!_inputManager)
        {
            Debug.LogError("InputManager Instance not found!");
            return;
        }
        _inputActions = _inputManager.PlayerControls;
        _moveAction = _inputActions.Player.Move;
        _jumpAction = _inputActions.Player.Jump;

        _jumpAction.performed += HandleJumpPerformed;
    }

    private void OnEnable()
    {
        if (_jumpAction != null)
        {
            _jumpAction.performed += HandleJumpPerformed;
        }
    }

    private void OnDisable()
    {
        _jumpAction.performed -= HandleJumpPerformed;
    }


    private void Update()
    {
        _moveInput = _moveAction.ReadValue<Vector2>();

        HandleRotation();
    }

    private void HandleRotation()
    {
        var pitchYaw = _inputActions.Player.Look.ReadValue<Vector2>();

        // pitchYaw *= lookSensitivity * Time.deltaTime;
        _currentRotationX -= pitchYaw.y * lookSensitivity;
        _currentRotationX = Mathf.Clamp(_currentRotationX, -90f, 90f);

        transform.Rotate(Vector3.up * (pitchYaw.x * lookSensitivity));

        head.localRotation = Quaternion.Euler(_currentRotationX, 0, 0);
    }

    private void FixedUpdate()
    {
        HandleMovementAndGravity();
    }

    private void HandleMovementAndGravity()
    {
        var isGrounded = _characterController.isGrounded;

        if (isGrounded && _playerVelocity.y < 0)
        {
            _playerVelocity.y = -2f;
        }

        var moveDirection = transform.forward * _moveInput.y + transform.right * _moveInput.x;
        moveDirection.Normalize();

        var horizontalVelocity = moveDirection * moveSpeed;

        _playerVelocity.x = horizontalVelocity.x;
        _playerVelocity.z = horizontalVelocity.z;


        if (_jumpRequested && isGrounded)
        {
            _playerVelocity.y = jumpForce;
            _jumpRequested = false;
        }

        _playerVelocity.y += _gravity * Time.fixedDeltaTime;


        _characterController.Move(_playerVelocity * Time.fixedDeltaTime);
    }

    private void HandleJumpPerformed(InputAction.CallbackContext context)
    {
        _jumpRequested = true;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var body = hit.collider.attachedRigidbody;

        if (!body || body.isKinematic)
            return;

        if (hit.moveDirection.y < -0.3f)
            return;

        var pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        body.linearVelocity = pushDir * 2.0f;
    }


    private void OnDrawGizmosSelected()
    {
        if (groundCheckTransform)
        {
            Gizmos.color = _characterController.isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckTransform.position, groundDistance);
        }
        else
        {
             Gizmos.color = _characterController.isGrounded ? Color.green : Color.red;
             Gizmos.DrawWireSphere(transform.position + Vector3.down * (_characterController.height / 2 - _characterController.radius), _characterController.radius);
        }
    }
}