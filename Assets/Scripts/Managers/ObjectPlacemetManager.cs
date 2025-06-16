#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Managers
{
    public class ObjectPlacementManager : MonoBehaviour
    {
        [Header("Placement Settings")]
        [SerializeField]
        [Tooltip("The list of prefabs that can be instantiated and placed.")]
        private List<GameObject> placeablePrefabs = new List<GameObject>();

        [SerializeField] [Tooltip("The layer(s) the object can be placed upon.")]
        private LayerMask placementLayerMask;

        [SerializeField] [Tooltip("Optional: Offset the placed object slightly above the surface.")]
        private float placementOffset = 0.05f;

        [Header("Visuals")] [SerializeField] [Tooltip("Color tint to apply while placing the object.")]
        private Color placementTint = new Color(1f, 0.5f, 0.5f, 0.75f);

        [SerializeField] [Tooltip("How far from the camera the object floats when not over a valid surface.")]
        private float defaultPlacementDistance = 1f;

        [Header("UI")] [SerializeField] [Tooltip("Assign the panel that contains the placeable object buttons.")]
        private GameObject? placementPanel;


        private InputManager? _inputManager;
        private Camera? _mainCamera;

        private GameObject? _currentPlacingObject;
        private int _selectedPrefabIndex = -1;
        private bool _isPlacing;

        private readonly List<Material> _cachedMaterials = new();
        private readonly List<Color> _originalColors = new();


        private void Awake()
        {
            _mainCamera = Camera.main;
            if (_mainCamera) return;

            Debug.LogError("ObjectPlacementManager requires a Camera tagged 'MainCamera' in the scene.", this);
            enabled = false;
        }

        private void Start()
        {
            _inputManager = InputManager.Instance;

            if (!_inputManager)
            {
                Debug.LogError("ObjectPlacementManager requires an InputManager instance in the scene.", this);
                enabled = false;
                return;
            }

            if (placementPanel)
            {
                placementPanel.SetActive(false);
            }
            else
            {
                Debug.LogWarning(
                    $"[{nameof(ObjectPlacementManager)}]: Placement Panel is not assigned in the Inspector. UI will not function.",
                    this);
            }

            // Start with the cursor locked for first-person controls
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // Check for 'P' key press to open the placement panel if not already placing.
            if (!_isPlacing && Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                ShowPlacementPanel();
            }

            if (!_isPlacing) return;

            HandlePlacementMovement();
            HandlePlacementConfirmationInput();
            HandlePlacementCancellationInput();
        }

        private void ShowPlacementPanel()
        {
            if (!placementPanel) return;
            var isPanelBeingOpened = !placementPanel.activeSelf;
            placementPanel.SetActive(isPanelBeingOpened);

            if (isPanelBeingOpened)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (!_isPlacing)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void SelectPrefabAndStartPlacing(int index)
        {
            if (index >= 0 && index < placeablePrefabs.Count)
            {
                if (placeablePrefabs[index])
                {
                    _selectedPrefabIndex = index;
                    Debug.Log($"Selected: {placeablePrefabs[index].name}");

                    if (placementPanel)
                    {
                        placementPanel.SetActive(false);
                    }

                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;

                    StartPlacing();
                }
                else
                {
                    Debug.LogWarning($"[{nameof(ObjectPlacementManager)}]: Prefab at index {index} is not assigned.",
                        this);
                }
            }
            else
            {
                Debug.LogWarning(
                    $"[{nameof(ObjectPlacementManager)}]: Invalid prefab index: {index}. List size is {placeablePrefabs.Count}.",
                    this);
            }
        }


        private void HandlePlacementMovement()
        {
            if (!_currentPlacingObject || !_mainCamera || Mouse.current == null) return;

            var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            var ray = _mainCamera.ScreenPointToRay(screenCenter);
            var currentPlacementDistance = defaultPlacementDistance;

            if (Physics.Raycast(ray, out var hit, 1000f, placementLayerMask))
            {
                _currentPlacingObject.transform.position = hit.point + hit.normal * placementOffset;
                _currentPlacingObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            else
            {
                _currentPlacingObject.transform.position = ray.GetPoint(currentPlacementDistance);
                _currentPlacingObject.transform.rotation = Quaternion.identity;
            }
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
            if (Mouse.current == null || Keyboard.current == null) return;
            if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelPlacing();
            }
        }


        private void StartPlacing()
        {
            if (_selectedPrefabIndex < 0 || _selectedPrefabIndex >= placeablePrefabs.Count ||
                !placeablePrefabs[_selectedPrefabIndex])
            {
                _isPlacing = false;
                return;
            }

            if (_currentPlacingObject)
            {
                Destroy(_currentPlacingObject);
                _cachedMaterials.Clear();
                _originalColors.Clear();
                _currentPlacingObject = null;
            }

            _isPlacing = true;
            _currentPlacingObject = Instantiate(placeablePrefabs[_selectedPrefabIndex]);
            Debug.Log($"Started placing object: {_currentPlacingObject.name}");

            if (_currentPlacingObject.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
            }

            if (_currentPlacingObject.TryGetComponent<Collider>(out var col))
            {
                col.enabled = false;
            }

            ApplyPlacementTint(_currentPlacingObject);
        }

        private void ConfirmPlacement()
        {
            if (!_isPlacing || !_currentPlacingObject) return;

            RemovePlacementTint();
            Debug.Log($"Placed object '{_currentPlacingObject.name}' at {_currentPlacingObject.transform.position}");

            if (_currentPlacingObject.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;
            }

            if (_currentPlacingObject.TryGetComponent<Collider>(out var col))
            {
                col.enabled = true;
            }

            _currentPlacingObject = null;
            _isPlacing = false;
            _selectedPrefabIndex = -1;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void CancelPlacing()
        {
            if (!_isPlacing || !_currentPlacingObject) return;

            Debug.Log("Placement cancelled.");
            Destroy(_currentPlacingObject);

            _cachedMaterials.Clear();
            _originalColors.Clear();

            if (placementPanel)
            {
                placementPanel.SetActive(false);
            }

            _currentPlacingObject = null;
            _isPlacing = false;
            _selectedPrefabIndex = -1;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ApplyPlacementTint(GameObject targetObject)
        {
            if (!targetObject) return;
            _cachedMaterials.Clear();
            _originalColors.Clear();
            var renderers = targetObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            foreach (var rend in renderers)
            {
                Material[] materialInstances = rend.materials;
                foreach (var matInstance in materialInstances)
                {
                    if (!matInstance) continue;

                    _cachedMaterials.Add(matInstance);
                    _originalColors.Add(matInstance.color);
                    matInstance.color = placementTint;
                }
            }
        }

        private void RemovePlacementTint()
        {
            if (_cachedMaterials.Count == 0 || _cachedMaterials.Count != _originalColors.Count)
            {
                _cachedMaterials.Clear();
                _originalColors.Clear();
                return;
            }

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