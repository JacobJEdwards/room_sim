using Interfaces;
using UnityEngine;

[RequireComponent(typeof(ImageUploader))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PlaceablePoster : MonoBehaviour, IInteractable, IHasName
{
    public string Name => "Poster";

    [Header("Placement Settings")]
    [SerializeField] private float wallDetectionDistance = 0.1f;
    [SerializeField] private LayerMask wallLayerMask = -1; // All layers by default
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float moveSmoothTime = 0.05f;

    [Header("Interaction Prompts")]
    [SerializeField] private string changeImagePromptDesktop = "Press E to change image";
    [SerializeField] private string changeImagePromptMobile = "Tap to change image";

    private ImageUploader _imageUploader;
    private Renderer _renderer;
    private Rigidbody _rigidbody;
    private Collider _collider;
    private Camera _mainCamera;

    private bool _isHeld;
    private bool _isPlacedOnWall;
    private Vector3 _targetPosition;
    private Vector3 _velocity = Vector3.zero;
    private float _heldDistance;
    private Vector3 _wallNormal;

    private void Awake()
    {
        _imageUploader = GetComponent<ImageUploader>();
        _renderer = GetComponent<Renderer>();
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _mainCamera = Camera.main;

        if (!_renderer)
        {
            Debug.LogError("A Renderer component is required on this object.", this);
            enabled = false;
            return;
        }

        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = true;
        _rigidbody.constraints = RigidbodyConstraints.None;

        if (_imageUploader != null)
        {
            _imageUploader.OnImageUploaded.AddListener(UpdateTexture);
        }
    }

    private void OnDestroy()
    {
        if (_imageUploader != null)
        {
            _imageUploader.OnImageUploaded.RemoveListener(UpdateTexture);
        }
    }

    private void Update()
    {
        if (!_isHeld) return;

        HandleHeldMovement();
        HandleRotation();
    }

    // --- DESKTOP ONLY MOUSE CONTROLS ---
    private void OnMouseDown()
    {
        // Only handle mouse clicks on desktop
        if (Managers.GameManager.IsMobilePlatform) return;
        
        if (!_isHeld)
        {
            PickupPoster();
        }
    }

    private void OnMouseUp()
    {
        // Only handle mouse clicks on desktop
        if (Managers.GameManager.IsMobilePlatform) return;
        
        if (_isHeld)
        {
            PlacePoster();
        }
    }

    // --- PUBLIC METHOD FOR MOBILE TOGGLE ---
    public void ToggleMovement()
    {
        if (_isHeld)
        {
            PlacePoster();
        }
        else
        {
            PickupPoster();
        }
    }

    private void HandleHeldMovement()
    {
        // Use viewport center for mobile, mouse position for desktop
        Ray ray;
        if (Managers.GameManager.IsMobilePlatform)
        {
            ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        }
        else
        {
            ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        }
        
        _targetPosition = ray.GetPoint(_heldDistance);

        transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _velocity, moveSmoothTime);

        if (Physics.Raycast(transform.position, _mainCamera.transform.forward, out RaycastHit hit, wallDetectionDistance, wallLayerMask))
        {
            _wallNormal = hit.normal;
            var targetRotation = Quaternion.LookRotation(-hit.normal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
        else
        {
            transform.rotation = _mainCamera.transform.rotation;
        }
    }

    private void HandleRotation()
    {
        // Only handle rotation on desktop
        if (Managers.GameManager.IsMobilePlatform) return;
        
        var scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            transform.Rotate(Vector3.forward, scrollInput * rotationSpeed * 10f, Space.Self);
        }
    }

    public void OnInteract(GameObject interactor)
    {
        _imageUploader.OpenFilePicker();
    }

    public bool CanInteract(GameObject interactor)
    {
        return !_isHeld;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        if (_isHeld) return "Moving... (Click to place)";
        return changeImagePromptDesktop;
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        if (_isHeld) return "Moving... (Tap Pickup button to place)";
        return changeImagePromptMobile;
    }

    private void PickupPoster()
    {
        _isHeld = true;
        _isPlacedOnWall = false;
        _rigidbody.isKinematic = false;
        
        // Use a reasonable default distance if the object is far away or just spawned
        float distance = Vector3.Distance(_mainCamera.transform.position, transform.position);
        _heldDistance = (distance < 0.5f || distance > 10f) ? 2f : distance;
        _velocity = Vector3.zero;
        
        // Update UI hint if available
        var uiManager = Managers.UIManager.Instance;
        if (uiManager != null)
        {
            if (Managers.GameManager.IsMobilePlatform)
                uiManager.SetHint("Moving... (Tap Pickup button to place)");
            else
                uiManager.SetHint("Click to place poster / Right-click to cancel");
        }
    }

    private void PlacePoster()
    {
        _isHeld = false;

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, wallDetectionDistance * 2f, wallLayerMask))
        {
            transform.position = hit.point - transform.forward * 0.01f;
            transform.rotation = Quaternion.LookRotation(-hit.normal);
            _isPlacedOnWall = true;
        }

        _rigidbody.isKinematic = true;
        _velocity = Vector3.zero;
        
        // Clear UI hint if available
        var uiManager = Managers.UIManager.Instance;
        if (uiManager != null)
        {
            uiManager.ClearHint();
        }
    }

    public void UpdateTexture(Texture2D newTexture)
    {
        if (_renderer && _renderer.material)
        {
            Debug.Log($"Updating texture on {gameObject.name}. New texture size: {newTexture.width}x{newTexture.height}");
            _renderer.material.mainTexture = newTexture;
        }
        else
        {
            Debug.LogError("Cannot update texture: Renderer or material is null");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_isHeld && _wallNormal != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, _wallNormal * 0.5f);
        }
    }

    public void ResetObject()
    {
        if (_isHeld)
        {
            _isHeld = false;
            _rigidbody.isKinematic = true;
            _velocity = Vector3.zero;
        }
    }
}