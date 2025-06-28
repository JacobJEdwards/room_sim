// Scripts/Managers/ObjectPlacementManager.cs

#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Managers
{
    public class ObjectPlacementManager : MonoBehaviour
    {
        public GameManager GameManager;
        private UIManager _uiManager;

        [Header("Placement Settings")]
        [SerializeField]
        [Tooltip("The list of prefabs that can be instantiated and placed.")]
        private List<GameObject> placeablePrefabs = new ();

        [SerializeField] [Tooltip("The layer(s) the object can be placed upon.")]
        private LayerMask placementLayerMask;

        [SerializeField] [Tooltip("Optional: Offset the placed object slightly above the surface.")]
        private float placementOffset = 0.05f;

        [Header("Visuals")]
        [SerializeField] [Tooltip("Color tint to apply while placing the object.")]
        private Color placementTint = new(1f, 0.5f, 0.5f, 0.75f);

        [SerializeField] [Tooltip("How far from the camera the object floats when not over a valid surface.")]
        private float defaultPlacementDistance = 1f;
        
        // Private script references
        private InputManager? _inputManager;
        private Camera? _mainCamera;

        // State variables
        private GameObject? _currentPlacingObject;
        private int _selectedPrefabIndex = -1;
        private bool _isPlacing;

        // Material caching for performance
        private readonly List<Material> _cachedMaterials = new();
        private readonly List<Color> _originalColors = new();

        private void Awake()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogError("ObjectPlacementManager requires a Camera tagged 'MainCamera' in the scene.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            // Get singleton instances
            GameManager = GameManager.Instance;
            _inputManager = InputManager.Instance;
            _uiManager = UIManager.Instance;

            if (!_inputManager || !_uiManager || !GameManager)
            {
                Debug.LogError("ObjectPlacementManager requires GameManager, InputManager, and UIManager instances in the scene.", this);
                enabled = false;
                return;
            }
            
            // Initial cursor state for FPS controls
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // This script's Update only runs while actively placing an object.
            if (!_isPlacing) return;

            HandlePlacementMovement();
            HandlePlacementConfirmationInput();
            HandlePlacementCancellationInput();
        }
        
        /// <summary>
        /// Called by UI buttons on the placement panel.
        /// </summary>
        public void SelectPrefabAndStartPlacing(int index)
        {
            if (index >= 0 && index < placeablePrefabs.Count)
            {
                if (placeablePrefabs[index] != null)
                {
                    _selectedPrefabIndex = index;

                    // Unambiguously close all UI to enter placement mode.
                    _uiManager.CloseAllPanels();
                    
                    // Start the placement process immediately after.
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

        /// <summary>
        /// Handles moving the ghost object with the camera.
        /// </summary>
        private void HandlePlacementMovement()
        {
            if (!_currentPlacingObject || !_mainCamera) return;

            var ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (Physics.Raycast(ray, out var hit, 1000f, placementLayerMask))
            {
                _currentPlacingObject.transform.position = hit.point + hit.normal * placementOffset;
                _currentPlacingObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            else
            {
                _currentPlacingObject.transform.position = ray.GetPoint(defaultPlacementDistance);
                _currentPlacingObject.transform.rotation = Quaternion.identity;
            }
        }

        private void HandlePlacementConfirmationInput()
        {
            if (_inputManager.PlayerControls.Player.Attack.WasPerformedThisFrame())
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

        /// <summary>
        /// Sets up the state for placing an object.
        /// </summary>
        private void StartPlacing()
        {
            // Set the game mode and cursor state for placement.
            GameManager.SetMode(GameManager.ControlMode.Placement);
    
            if (_selectedPrefabIndex < 0)
            {
                _isPlacing = false;
                return;
            }

            if (_currentPlacingObject) Destroy(_currentPlacingObject);

            _isPlacing = true;
            _currentPlacingObject = Instantiate(placeablePrefabs[_selectedPrefabIndex]);

            // Disable physics on the ghost object.
            if (_currentPlacingObject.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
            if (_currentPlacingObject.TryGetComponent<Collider>(out var col)) col.enabled = false;

            ApplyPlacementTint(_currentPlacingObject);
        }

        /// <summary>
        /// Finalizes the object's position and restores its properties.
        /// </summary>
        private void ConfirmPlacement()
        {
            if (!_isPlacing || !_currentPlacingObject) return;

            RemovePlacementTint();

            // Re-enable physics.
            if (_currentPlacingObject.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;
            if (_currentPlacingObject.TryGetComponent<Collider>(out var col)) col.enabled = true;

            // Reset state and return to camera mode.
            _currentPlacingObject = null;
            _isPlacing = false;
            _selectedPrefabIndex = -1;
            
            GameManager.SetMode(GameManager.ControlMode.Camera);
        }

        /// <summary>
        /// Aborts the placement process and destroys the ghost object.
        /// </summary>
        private void CancelPlacing()
        {
            if (!_isPlacing || !_currentPlacingObject) return;

            Destroy(_currentPlacingObject);
            _cachedMaterials.Clear();
            _originalColors.Clear();

            // When cancelling, we want to re-open the placement panel.
            _uiManager.TogglePlacementPanel();

            _currentPlacingObject = null;
            _isPlacing = false;
            _selectedPrefabIndex = -1;
            
            // The UIManager and GameManager will handle the mode and cursor state.
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
                    // --- THIS IS THE FIX ---
                    // Check if the material's shader has a "_Color" property before trying to access it.
                    if (matInstance && matInstance.HasProperty("_Color"))
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