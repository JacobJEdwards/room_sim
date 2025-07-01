using System;
using Interfaces;
using Managers;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class MoveableObject : MonoBehaviour, IInteractable
{
    [Header("Movement Settings")]
    [SerializeField]
    [Tooltip("How fast the object rotates while held.")]
    private float rotationSpeed = 50f;
    [SerializeField]
    [Tooltip("How smoothly the object follows the mouse position (lower values are smoother but lag more).")]
    private float moveSmoothTime = 0.005f;
    [SerializeField]
    [Tooltip("A small buffer to allow for slight clipping, preventing the object from getting stuck.")]
    private float skinWidth = 0.05f;

    [Header("Rotation Axes")]
    [SerializeField]
    [Tooltip("The axis of rotation for the Left and Right arrow keys.")]
    private Vector3 horizontalRotationAxis = Vector3.up;
    [SerializeField]
    [Tooltip("The axis of rotation for the Up and Down arrow keys.")]
    private Vector3 verticalRotationAxis = Vector3.right;


    [Header("Interaction")]
    [SerializeField]
    [Tooltip("Text displayed when the object can be picked up.")]
    private string pickupPrompt = "Double Click to pick up";
    [SerializeField]
    [Tooltip("Text displayed when the object can be picked up on mobile.")]
    private string pickupPromptMobile = "Tap to pick up";

    private Rigidbody _rigidbody;
    private Collider _collider;
    private Camera _mainCamera;
    private bool _isHeld;
    private Vector3 _targetPosition;
    private Vector3 _velocity = Vector3.zero;
    private float _heldDistance;
    private InputManager _inputManager;
    private UIManager _uiManager;

    private bool _leftArrowPressed;
    private bool _rightArrowPressed;
    private bool _upArrowPressed;
    private bool _downArrowPressed;
    private bool _commaPressed;
    private bool _dotPressed;

    private Vector3 _scrollRotationAxis = Vector3.up;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _rigidbody.useGravity = true;
        _rigidbody.isKinematic = false;

         _rigidbody.constraints = RigidbodyConstraints.None;

        _mainCamera = Camera.main;
        if (!_mainCamera)
        {
            Debug.LogError("MoveableObject requires a Camera tagged 'MainCamera' in the scene.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        _inputManager = InputManager.Instance;
        _uiManager = UIManager.Instance;

        _inputManager.SetOnLeftArrowPressed(() => { if (_isHeld) _leftArrowPressed = true; });
        _inputManager.SetOnLeftArrowReleased(() => { _leftArrowPressed = false; });
        _inputManager.SetOnRightArrowPressed(() => { if (_isHeld) _rightArrowPressed = true; });
        _inputManager.SetOnRightArrowReleased(() => { _rightArrowPressed = false; });
        _inputManager.SetOnUpArrowPressed(() => { if (_isHeld) _upArrowPressed = true; });
        _inputManager.SetOnUpArrowReleased(() => { _upArrowPressed = false; });
        _inputManager.SetOnDownArrowPressed(() => { if (_isHeld) _downArrowPressed = true; });
        _inputManager.SetOnDownArrowReleased(() => { _downArrowPressed = false; });
        _inputManager.SetOnCommaPressed(() => { if (_isHeld) _commaPressed = true; });
        _inputManager.SetOnCommaReleased(() => { _commaPressed = false; });
        _inputManager.SetOnDotPressed(() => { if (_isHeld) _dotPressed = true; });
        _inputManager.SetOnDotReleased(() => { _dotPressed = false; });
    }

    private void OnMouseDown()
    {
        if (_isHeld) Drop();
        else Pickup();
    }

    private void FixedUpdate()
    {
        if (!_isHeld) return;

        var smoothedPosition = Vector3.SmoothDamp(transform.position, _targetPosition, ref _velocity, moveSmoothTime);
        var direction = smoothedPosition - transform.position;
        var distance = direction.magnitude;
        var castExtents = _collider.bounds.extents - Vector3.one * skinWidth;

        if (!Physics.BoxCast(transform.position, castExtents, direction.normalized, transform.rotation, distance))
        {
            _rigidbody.MovePosition(smoothedPosition);
        }
        
        if (_leftArrowPressed) transform.Rotate(horizontalRotationAxis, rotationSpeed * Time.deltaTime);
        if (_rightArrowPressed) transform.Rotate(horizontalRotationAxis, -rotationSpeed * Time.deltaTime);
        if (_upArrowPressed) transform.Rotate(verticalRotationAxis, rotationSpeed * Time.deltaTime);
        if (_downArrowPressed) transform.Rotate(verticalRotationAxis, -rotationSpeed * Time.deltaTime);

        if (_commaPressed)
        {
            _heldDistance -= Time.deltaTime;
            _heldDistance = Mathf.Clamp(_heldDistance, 0.5f, 10f);
        }

        if (!_dotPressed) return;
        _heldDistance += Time.deltaTime;
        _heldDistance = Mathf.Clamp(_heldDistance, 0.5f, 10f);
    }

    private void Update()
    {
        if (!_isHeld) return;
        
        if (Input.GetKeyDown(KeyCode.Alpha1)) _scrollRotationAxis = Vector3.right;
        if (Input.GetKeyDown(KeyCode.Alpha2)) _scrollRotationAxis = Vector3.up;
        if (Input.GetKeyDown(KeyCode.Alpha3)) _scrollRotationAxis = Vector3.forward;
        
        var scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            transform.Rotate(_scrollRotationAxis, scrollInput * rotationSpeed * 10f, Space.Self);
        }

        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        _targetPosition = ray.GetPoint(_heldDistance);
    }

    private void Pickup()
    {
        _isHeld = true;
        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = true; 
        _heldDistance = Vector3.Distance(_mainCamera.transform.position, transform.position);
        _velocity = Vector3.zero;
        _targetPosition = transform.position;
        _uiManager.ShowHoldingPanel();
    }

    private void Drop()
    {
        _isHeld = false;
        _rigidbody.useGravity = true;
        _rigidbody.isKinematic = false;
        _uiManager.HideHoldingPanel();
    }
    
    public void OnInteract(GameObject interactor)
    {
        if (_isHeld) Drop();
        else Pickup();
    }

    public bool CanInteract(GameObject interactor)
    {
        // *** FIX: Return false when held to prevent interaction prompt from showing ***
        return !_isHeld;
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        // This will now only be called when the object is not held.
        return pickupPromptMobile;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        // This will now only be called when the object is not held.
        return pickupPrompt;
    }
}