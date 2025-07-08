using Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input Settings")]
    private InputSystem _inputActions;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _lookAction;
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

    [SerializeField]
    private CharacterController characterController;
    private InputManager _inputManager = null!;
    private Vector2 _moveInput;
    private bool _jumpRequested;

    private float _currentRotationX;
    private Vector3 _playerVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
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
        _lookAction = _inputActions.Player.Look;

        _jumpAction.performed += HandleJumpPerformed;
        if (UnityEngine.Device.Application.isMobilePlatform)
        {
            _lookAction.performed += FilterMobileLookInput;
        }
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        lookSensitivity = sensitivity / 100f;
    }

    private void OnEnable()
    {
        if (_jumpAction != null)
        {
            _jumpAction.performed += HandleJumpPerformed;
        }
        
        if (UnityEngine.Device.Application.isMobilePlatform && _lookAction != null)
        {
            _lookAction.performed += FilterMobileLookInput;
        }
    }

    private void OnDisable()
    {
        _jumpAction.performed -= HandleJumpPerformed;
        
        if (UnityEngine.Device.Application.isMobilePlatform && _lookAction != null)
        {
            _lookAction.performed -= FilterMobileLookInput;
        }
    }

    private void FilterMobileLookInput(InputAction.CallbackContext context)
    {
        // Check if the input is coming from a touchscreen
        if (context.control.device is Touchscreen)
        {
            // Cancel the touch input by not processing it
            context.ReadValueAsObject();
            return;
        }
    }

    private void Update()
    {
        _moveInput = _moveAction.ReadValue<Vector2>();

        HandleRotation();
    }

    private void HandleRotation()
    {
        // On mobile, ignore look input if it's from touch
        if (UnityEngine.Device.Application.isMobilePlatform)
        {
            // Check the active control to see if it's touch-based
            var lookControl = _lookAction.activeControl;
            if (lookControl != null && lookControl.device is Touchscreen)
            {
                return; // Ignore touch input
            }
        }

        var pitchYaw = _lookAction.ReadValue<Vector2>();

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
        var isGrounded = characterController.isGrounded;

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


        characterController.Move(_playerVelocity * Time.fixedDeltaTime);
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
            Gizmos.color = characterController.isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckTransform.position, groundDistance);
        }
        else
        {
             Gizmos.color = characterController.isGrounded ? Color.green : Color.red;
             Gizmos.DrawWireSphere(transform.position + Vector3.down * (characterController.height / 2 - characterController.radius), characterController.radius);
        }
    }
}