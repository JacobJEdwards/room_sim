using System;
using Interfaces;
using Managers; // Assuming this namespace exists and contains IInteractable
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class MoveableObject : MonoBehaviour, IInteractable
{
    [Header("Movement Settings")]
    [SerializeField]
    [Tooltip("How fast the object rotates while held.")]
    private float rotationSpeed = 100f;
    [SerializeField]
    [Tooltip("How smoothly the object follows the mouse position (lower values are smoother but lag more).")]
    private float moveSmoothTime = 0.05f;

    [Header("Interaction")]
    [SerializeField]
    [Tooltip("Text displayed when the object can be picked up.")]
    private string pickupPrompt = "Click to pick up";
    [SerializeField]
    [Tooltip("Text displayed when the object is being held.")]
    private string dropPrompt = "Click to drop | Scroll to rotate";

    private Rigidbody _rigidbody;
    private Camera _mainCamera;
    private bool _isHeld;
    private Vector3 _targetPosition;
    private Vector3 _velocity = Vector3.zero;
    private float _heldDistance;
    private InputManager _inputManager;

    private bool _leftArrowPressed;
    private bool _rightArrowPressed;
    private bool _upArrowPressed;
    private bool _downArrowPressed;
    private bool _commaPressed;
    private bool _dotPressed;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = true;
        _rigidbody.isKinematic = false;

         _rigidbody.constraints = RigidbodyConstraints.None;

        _mainCamera = Camera.main;
        if (_mainCamera) return;

        Debug.LogError("MoveableObject requires a Camera tagged 'MainCamera' in the scene.", this);
        enabled = false;
    }

    private void Start()
    {
        _inputManager = InputManager.Instance;

        _inputManager.SetOnLeftArrowPressed(() =>
        {
            if (_isHeld)
            {
                _leftArrowPressed = true;
            }
        });

        _inputManager.SetOnLeftArrowReleased(() =>
        {
            _leftArrowPressed = false;
        });

        _inputManager.SetOnRightArrowPressed(() =>
        {
            if (_isHeld)
            {
                _rightArrowPressed = true;
            }
        });
        _inputManager.SetOnRightArrowReleased(() =>
        {
            _rightArrowPressed = false;
        });

        _inputManager.SetOnUpArrowPressed(() =>
        {
            if (_isHeld)
            {
                _upArrowPressed = true;
            }
        });
        _inputManager.SetOnUpArrowReleased(() =>
        {
            _upArrowPressed = false;
        });

        _inputManager.SetOnDownArrowPressed(() =>
        {
            if (_isHeld)
            {
                _downArrowPressed = true;
            }
        });
        _inputManager.SetOnDownArrowReleased(() =>
        {
            _downArrowPressed = false;
        });

        _inputManager.SetOnCommaPressed(() =>
        {
            if (_isHeld)
            {
                _commaPressed = true;
            }
        });

        _inputManager.SetOnCommaReleased(() =>
        {
            _commaPressed = false;
        });

        _inputManager.SetOnDotPressed(() =>
        {
            if (_isHeld)
            {
                _dotPressed = true;
            }
        });

        _inputManager.SetOnDotReleased(() =>
        {
            _dotPressed = false;
        });
    }

    private void OnMouseDown()
    {
        if (!_isHeld)
        {
            Pickup();
        }
    }

    private void OnMouseUp()
    {
        if (_isHeld)
        {
            Drop();
        }
    }

    private void FixedUpdate()
    {
        if (!_isHeld) return;

        var smoothedPosition = Vector3.SmoothDamp(_rigidbody.position, _targetPosition, ref _velocity, moveSmoothTime);
        _rigidbody.MovePosition(smoothedPosition);

        if (_leftArrowPressed)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
        if (_rightArrowPressed)
        {
            transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
        }
        if (_upArrowPressed)
        {
            transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime);
        }
        if (_downArrowPressed)
        {
            transform.Rotate(Vector3.right, -rotationSpeed * Time.deltaTime);
        }

        if (_commaPressed)
        {
            _heldDistance -= Time.deltaTime;
            _heldDistance = Mathf.Clamp(_heldDistance, 0.5f, 10f);
        }

        if (_dotPressed)
        {
            _heldDistance += Time.deltaTime;
            _heldDistance = Mathf.Clamp(_heldDistance, 0.5f, 10f);
        }
    }

    private void Update()
    {
        if (!_isHeld) return;

        var scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            transform.Rotate(Vector3.up, scrollInput * rotationSpeed * Time.deltaTime, Space.Self);
        }

        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        _targetPosition = ray.GetPoint(_heldDistance);

        // Optional: Add collision checks here to prevent dragging through walls
    }

    private void Pickup()
    {
        _isHeld = true;
        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = true;

        _heldDistance = Vector3.Distance(_mainCamera.transform.position, transform.position);

        _velocity = Vector3.zero;

        _targetPosition = transform.position;
    }

    private void Drop()
    {
        _isHeld = false;
        _rigidbody.useGravity = true;
        _rigidbody.isKinematic = false;

    }


    public void OnInteract(GameObject interactor)
    {
        if (_isHeld) Drop();
        else Pickup();
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPrompt(GameObject interactor)
    {
        return _isHeld ? dropPrompt : pickupPrompt;
    }

}