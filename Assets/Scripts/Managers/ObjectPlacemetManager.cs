// Scripts/Managers/ObjectPlacementManager.cs

#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Managers
{
    public class ObjectPlacementManager : MonoBehaviour
    {
        private static readonly int Color1 = Shader.PropertyToID("_Color");
        public GameManager GameManager = null!;
        private UIManager _uiManager = null!;

        [Header("Placement Settings")]
        [SerializeField]
        [Tooltip("The list of prefabs that can be instantiated and placed.")]
        private List<GameObject> placeablePrefabs = new ();

        [SerializeField] [Tooltip("The layer(s) the object can be placed upon.")]
        private LayerMask placementLayerMask;

        [SerializeField] [Tooltip("The tag that will be used for collision detection.")]
        private string collisionTag = "Wall";
        
        [SerializeField] [Tooltip("The tag that will be used to ignore collision.")]
        private string ignoreTag = "Player";

        [SerializeField] [Tooltip("Optional: Offset the placed object slightly above the surface.")]
        private float placementOffset = 0.05f;

        [Header("Visuals")]
        [SerializeField] [Tooltip("Color tint to apply while placing the object.")]
        private Color placementTint = new(1f, 0.5f, 0.5f, 0.75f);

        [SerializeField] [Tooltip("How far from the camera the object floats when not over a valid surface.")]
        private float defaultPlacementDistance = 1f;
        
        private InputManager? _inputManager;
        private Camera? _mainCamera;

        private GameObject? _currentPlacingObject;
        private int _selectedPrefabIndex = -1;
        private bool _isPlacing;
        
        private Collider _placingObjectCollider;


        private readonly List<Material> _cachedMaterials = new();
        private readonly List<Color> _originalColors = new();

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            GameManager = GameManager.Instance;
            _inputManager = InputManager.Instance;
            _uiManager = UIManager.Instance;

            if (!_inputManager || !_uiManager || !GameManager)
            {
                Debug.LogError("ObjectPlacementManager requires GameManager, InputManager, and UIManager instances in the scene.", this);
                enabled = false;
                return;
            }
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (!_isPlacing) return;

            HandlePlacementMovement();
            HandlePlacementConfirmationInput();
            HandlePlacementCancellationInput();
        }

        public void SelectRandom()
        {
            SelectPrefabAndStartPlacing(Random.Range(0, placeablePrefabs.Count));
        }
        
        public void SelectPrefabAndStartPlacing(int index)
        {
            if (index >= 0 && index < placeablePrefabs.Count)
            {
                if (placeablePrefabs[index])
                {
                    _selectedPrefabIndex = index;
                    _uiManager.CloseAllPanels();
                    StartPlacing();
                }
                else
                {
                    Debug.LogWarning($"[{nameof(ObjectPlacementManager)}]: Prefab at index {index} is not assigned.", this);
                }
            }
            else
            {
                Debug.LogWarning($"[{nameof(ObjectPlacementManager)}]: Invalid prefab index: {index}. List size is {placeablePrefabs.Count}.", this);
            }
        }
        
        private void HandlePlacementMovement()
        {
            if (!_currentPlacingObject || !_mainCamera) return;
        
            var ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 targetPosition;
            Quaternion targetRotation;
        
            if (Physics.Raycast(ray, out var hit, 1000f, placementLayerMask))
            {
                targetPosition = hit.point + hit.normal * placementOffset;
                targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            else
            {
                targetPosition = ray.GetPoint(defaultPlacementDistance);
                targetRotation = _mainCamera.transform.rotation;
            }
        
            var direction = targetPosition - _currentPlacingObject.transform.position;
            var distance = direction.magnitude;
        
            if (_placingObjectCollider != null &&
                Physics.BoxCast(
                    _currentPlacingObject.transform.position,
                    _placingObjectCollider.bounds.extents,
                    direction.normalized,
                    out var boxHit,
                    _currentPlacingObject.transform.rotation,
                    distance) && 
                boxHit.collider.CompareTag(collisionTag) && 
                !boxHit.collider.CompareTag(ignoreTag))

            {
                // Don't move if there's a collision with the specified tag
            }
            else
            {
                _currentPlacingObject.transform.position = targetPosition;
            }
        
            _currentPlacingObject.transform.rotation = targetRotation;
        }


        private void HandlePlacementConfirmationInput()
        {
            if (_inputManager && _inputManager.PlayerControls.Player.Attack.WasPerformedThisFrame())
            {
                ConfirmPlacement();
            }
        }

        private void HandlePlacementCancellationInput()
        {
            if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelPlacing();
            }
        }
        
        private void StartPlacing()
        {
            GameManager.SetMode(GameManager.ControlMode.Placement);
            var currentRoom = GameManager.CurrentRoom;
    
            if (_selectedPrefabIndex < 0)
            {
                _isPlacing = false;
                return;
            }

            if (_currentPlacingObject) Destroy(_currentPlacingObject);

            _isPlacing = true;
            _currentPlacingObject = Instantiate(placeablePrefabs[_selectedPrefabIndex], currentRoom.transform);

            if (_currentPlacingObject.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
            if (_currentPlacingObject.TryGetComponent<Collider>(out var col))
            {
                col.isTrigger = true;
                _placingObjectCollider = col;
            }

            ApplyPlacementTint(_currentPlacingObject);
            _uiManager.ShowHoldingPanel();
        }
        
        private void ConfirmPlacement()
        {
            if (!_isPlacing || !_currentPlacingObject) return;

            RemovePlacementTint();
            if (_currentPlacingObject.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;
            if (_currentPlacingObject.TryGetComponent<Collider>(out var col))
            {
                col.isTrigger = false;
            }
            GameManager.CurrentRoom.AddPlacedObject(_currentPlacingObject);

            _uiManager.HideHoldingPanel();

            _currentPlacingObject = null;
            _isPlacing = false;
            _selectedPrefabIndex = -1;
            
            GameManager.SetMode(GameManager.ControlMode.Camera);

        }

        private void CancelPlacing()
        {
            if (!_isPlacing || !_currentPlacingObject) return;

            Destroy(_currentPlacingObject);
            _cachedMaterials.Clear();
            _originalColors.Clear();
            
            _uiManager.HideHoldingPanel();
            _uiManager.TogglePlacementPanel();

            _currentPlacingObject = null;
            _isPlacing = false;
            _selectedPrefabIndex = -1;
        }

        private void ApplyPlacementTint(GameObject targetObject)
        {
            if (!targetObject) return;
            _cachedMaterials.Clear();
            _originalColors.Clear();
            var renderers = targetObject.GetComponentsInChildren<Renderer>();

            foreach (var rend in renderers)
            {
                foreach (var matInstance in rend.materials)
                {
                    if (!matInstance || !matInstance.HasProperty(Color1)) continue;

                    _cachedMaterials.Add(matInstance);
                    _originalColors.Add(matInstance.color);
                    matInstance.color = placementTint;
                }
            }
        }

        private void RemovePlacementTint()
        {
            if (_cachedMaterials.Count != _originalColors.Count) return;

            for (var i = 0; i < _cachedMaterials.Count; i++)
            {
                if (_cachedMaterials[i])
                {
                    _cachedMaterials[i].color = _originalColors[i];
                }
            }
            _cachedMaterials.Clear();
            _originalColors.Clear();
        }
    }
}