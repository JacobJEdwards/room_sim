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
        
        [SerializeField] [Tooltip("How far from the camera the object floats when not over a valid surface.")]
        private float defaultPlacementDistance = 3f;

        [Header("Visuals")]
        [SerializeField] [Tooltip("Color tint to apply while placing the object.")]
        private Color placementTint = new(1f, 0.5f, 0.5f, 0.75f);
        
        private InputManager? _inputManager;
        private Camera? _mainCamera;

        private GameObject? _currentPlacingObject;
        private int _selectedPrefabIndex = -1;
        private bool _isPlacingNonMoveable;
        
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
            }
        }

        private void Update()
        {
            if (!_isPlacingNonMoveable) return;

            HandleNonMoveablePlacement();
            HandlePlacementConfirmationInput();
            HandlePlacementCancellationInput();
        }
        
        public void SelectPrefabAndStartPlacing(int index)
        {
            if (index < 0 || index >= placeablePrefabs.Count || placeablePrefabs[index] == null)
            {
                Debug.LogWarning($"Invalid prefab index: {index}", this);
                return;
            }

            var curRoom = GameManager.CurrentRoom;

            _uiManager.CloseAllPanels();
            
            var prefabToPlace = placeablePrefabs[index];
            
            var spawnPos = _mainCamera.transform.position + (_mainCamera.transform.forward * 1.5f);
            
            var newObject = Instantiate(prefabToPlace, spawnPos, Quaternion.identity, curRoom.transform);
            curRoom.AddPlacedObject(newObject);

            if (newObject.TryGetComponent<MoveableObject>(out var moveable))
            {
                moveable.Pickup(isNewlySpawned: true);
            }
            else
            {
                _selectedPrefabIndex = index;
                StartPlacingNonMoveable(newObject);
            }
        }

        private void StartPlacingNonMoveable(GameObject newObject)
        {
            _isPlacingNonMoveable = true;
            _currentPlacingObject = newObject;

            if (_currentPlacingObject.TryGetComponent<Collider>(out var col))
            {
                col.enabled = false;
            }
            
            GameManager.SetMode(GameManager.ControlMode.Placement);
            ApplyPlacementTint(_currentPlacingObject);
            _uiManager.SetHint("Click to place poster / Right-click to cancel");
        }

        private void HandleNonMoveablePlacement()
        {
            if (!_currentPlacingObject || !_mainCamera) return;

            var ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            
            if (Physics.Raycast(ray, out var hit, 100f, placementLayerMask))
            {
                _currentPlacingObject.transform.position = hit.point + hit.normal * 0.01f;
                _currentPlacingObject.transform.rotation = Quaternion.LookRotation(-hit.normal);
            }
            else
            {
                _currentPlacingObject.transform.position = ray.GetPoint(defaultPlacementDistance);
                _currentPlacingObject.transform.rotation = _mainCamera.transform.rotation;
            }
        }

        private void HandlePlacementConfirmationInput()
        {
            if (_inputManager?.PlayerControls.Player.Attack.WasPerformedThisFrame() ?? false)
            {
                if (_isPlacingNonMoveable)
                {
                    ConfirmNonMoveablePlacement();
                }
            }
        }

        private void HandlePlacementCancellationInput()
        {
            if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_isPlacingNonMoveable)
                {
                    CancelNonMoveablePlacement();
                }
            }
        }

        private void ConfirmNonMoveablePlacement()
        {
            if (!_currentPlacingObject) return;
            
            RemovePlacementTint();

            if (_currentPlacingObject.TryGetComponent<Collider>(out var col))
            {
                col.enabled = true;
            }
            
            GameManager.CurrentRoom.AddPlacedObject(_currentPlacingObject);
            _uiManager.ClearHint();
            
            _currentPlacingObject = null;
            _isPlacingNonMoveable = false;
            _selectedPrefabIndex = -1;
            
            GameManager.SetMode(GameManager.ControlMode.Camera);
        }

        private void CancelNonMoveablePlacement()
        {
            if (!_currentPlacingObject) return;

            Destroy(_currentPlacingObject);
            _uiManager.ClearHint();
            
            _currentPlacingObject = null;
            _isPlacingNonMoveable = false;
            _selectedPrefabIndex = -1;
            
            GameManager.SetMode(GameManager.ControlMode.Camera);
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
                    if (matInstance && matInstance.HasProperty(Color1))
                    {
                        _cachedMaterials.Add(matInstance);
                        _originalColors.Add(matInstance.color);
                        matInstance.color = placementTint;
                    }
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