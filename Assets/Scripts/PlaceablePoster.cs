using Interfaces;
using UnityEngine;

[RequireComponent(typeof(ImageUploader))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PlaceablePoster : MonoBehaviour, IInteractable
{
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

    private void OnMouseDown()
    {
        if (!_isHeld)
        {
            PickupPoster();
        }
    }

    private void OnMouseUp()
    {
        if (_isHeld)
        {
            PlacePoster();
        }
    }

    private void HandleHeldMovement()
    {
        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
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
        return changeImagePromptDesktop;
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return changeImagePromptMobile;
    }

    private void PickupPoster()
    {
        _isHeld = true;
        _isPlacedOnWall = false;
        _rigidbody.isKinematic = false;
        _heldDistance = Vector3.Distance(_mainCamera.transform.position, transform.position);
        _velocity = Vector3.zero;
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
    }
}