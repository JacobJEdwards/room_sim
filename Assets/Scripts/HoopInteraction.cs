// Scripts/HoopInteraction.cs
using Interfaces;
using Managers;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class HoopInteraction : MonoBehaviour, IInteractable, IHasName
{
    public string Name => "Basketball Hoop";

    [Header("Movement Settings")]
    [SerializeField] private float moveSmoothTime = 0.05f;
    [SerializeField] private float rotationSpeed = 100f;

    // --- Core Components ---
    private Rigidbody _rigidbody;
    private Camera _mainCamera;

    // --- State Control ---
    private bool _isHeld;

    // --- Movement Data ---
    private Vector3 _velocity = Vector3.zero;
    private float _heldDistance;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _mainCamera = Camera.main;

        // Store the starting position and rotation to reset to
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
    }

    private void Update()
    {
        // Only handle movement logic if the object is being held
        if (!_isHeld) return;
        HandleHeldMovement();
        HandleRotation();
    }

    // --- Desktop Mouse Controls ---
    private void OnMouseDown()
    {
        // On desktop, clicking picks up the hoop if it's not already held
        if (GameManager.IsMobilePlatform || _isHeld || BasketballManager.Instance.IsInBasketballMode()) return;
        PickupHoop();
    }

    private void OnMouseUp()
    {
        // On desktop, releasing the click places the hoop
        if (GameManager.IsMobilePlatform || !_isHeld) return;
        PlaceHoop();
    }

    // --- Public method for Mobile Controls ---
    public void ToggleMovement()
    {
        // This is called by the InteractionManager on mobile
        if (BasketballManager.Instance.IsInBasketballMode()) return;
        
        if (_isHeld)
        {
            PlaceHoop();
        }
        else
        {
            PickupHoop();
        }
    }

    private void HandleHeldMovement()
    {
        // Determine the target position based on the center of the screen
        Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPosition = ray.GetPoint(_heldDistance);
        
        // Use Rigidbody.MovePosition for smooth, physics-compliant movement
        _rigidbody.MovePosition(Vector3.SmoothDamp(_rigidbody.position, targetPosition, ref _velocity, moveSmoothTime));
    }
    
    private void HandleRotation()
    {
        // Desktop-only scroll wheel rotation
        if (GameManager.IsMobilePlatform) return;
        
        var scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            Quaternion deltaRotation = Quaternion.Euler(Vector3.up * (scrollInput * rotationSpeed * 10f * Time.deltaTime));
            _rigidbody.MoveRotation(_rigidbody.rotation * deltaRotation);
        }
    }

    private void PickupHoop()
    {
        _isHeld = true;
        // This is the key: make the Rigidbody kinematic so it's immune to physics
        _rigidbody.isKinematic = true; 
        
        float distance = Vector3.Distance(_mainCamera.transform.position, transform.position);
        _heldDistance = Mathf.Clamp(distance, 2f, 15f);
        _velocity = Vector3.zero;
    }

    private void PlaceHoop()
    {
        _isHeld = false;
        // Return the Rigidbody to its default non-kinematic state
        _rigidbody.isKinematic = false; 
        _velocity = Vector3.zero;
    }

    // --- IInteractable Implementation ---
    public void OnInteract(GameObject interactor)
    {
        // This is the primary action: playing basketball
        if (BasketballManager.Instance != null)
        {
            if (BasketballManager.Instance.IsInBasketballMode())
            {
                BasketballManager.Instance.ExitShootingMode();
            }
            else
            {
                BasketballManager.Instance.StartShootingMode();
            }
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        // Can't play basketball while moving the hoop
        return !_isHeld && (GameManager.Instance.CurrentMode == GameManager.ControlMode.Camera ||
                            GameManager.Instance.CurrentMode == GameManager.ControlMode.Basketball);
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        if (_isHeld) return "Click to place hoop";

        string prompt = BasketballManager.Instance.IsInBasketballMode() ? "Press E to stop playing" : "Press E to play basketball";
        prompt += " | Click to move";
        return prompt;
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        if (_isHeld) return "Tap Pickup button to place";
        
        string prompt = BasketballManager.Instance.IsInBasketballMode() ? "Tap to stop playing" : "Tap to play basketball";
        prompt += " | Tap Pickup to move";
        return prompt;
    }

    public void ResetObject()
    {
        // Reset everything to its initial state
        if (BasketballManager.Instance != null && BasketballManager.Instance.IsInBasketballMode())
        {
            BasketballManager.Instance.ExitShootingMode();
        }

        if (_isHeld)
        {
            PlaceHoop();
        }
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }
}