#nullable enable

using UnityEngine;
using Managers;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

public class ObjectPlacementManager : MonoBehaviour
{
    [Header("Placement Settings")]
    [SerializeField]
    [Tooltip("The list of prefabs that can be instantiated and placed.")]
    private List<GameObject> placeablePrefabs = new List<GameObject>();

    [SerializeField]
    [Tooltip("The display names corresponding to the prefabs above (ensure same order and size!).")]
    private List<string> placeableObjectNames = new List<string>();

    [SerializeField]
    [Tooltip("The layer(s) the object can be placed upon.")]
    private LayerMask placementLayerMask;

    [SerializeField]
    [Tooltip("Optional: Offset the placed object slightly above the surface.")]
    private float placementOffset = 0.05f;

    [Header("Visuals")]
    [SerializeField]
    [Tooltip("Color tint to apply while placing the object.")]
    private Color placementTint = new Color(1f, 0.5f, 0.5f, 0.75f);

    [SerializeField]
    [Tooltip("How far from the camera the object floats when not over a valid surface.")]
    private float defaultPlacementDistance = 1f;

    [Header("UI Display")]
    [SerializeField]
    [Tooltip("Assign the TextMeshPro UI element here to display the selected object name.")]
    private TextMeshProUGUI? selectionTextDisplay;

    private InputManager? _inputManager;
    private Camera? _mainCamera;

    private GameObject? _currentPlacingObject;
    private int _selectedPrefabIndex = -1;
    private bool _isPlacing;

    private readonly List<Material> _cachedMaterials = new List<Material>();
    private readonly List<Color> _originalColors = new List<Color>();


    private void Awake()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("ObjectPlacementManager requires a Camera tagged 'MainCamera' in the scene.", this);
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        _inputManager = InputManager.Instance;
        if (_inputManager == null)
        {
             Debug.LogError("ObjectPlacementManager requires an InputManager instance in the scene.", this);
             enabled = false;
             return;
        }

        if (selectionTextDisplay != null)
        {
            selectionTextDisplay.gameObject.SetActive(false);
            selectionTextDisplay.text = "";
        } else {
             Debug.LogWarning($"[{nameof(ObjectPlacementManager)}]: Selection Text Display is not assigned in the Inspector. Selection text will not be shown.", this);
        }


        if(placeablePrefabs.Count != placeableObjectNames.Count)
        {
            Debug.LogWarning($"[{nameof(ObjectPlacementManager)}]: Mismatch between placeablePrefabs list ({placeablePrefabs.Count}) and placeableObjectNames list ({placeableObjectNames.Count}). UI names might be incorrect.", this);
        }
    }

    private void Update()
    {
        if (!_isPlacing)
        {
             HandleSelectionInput();
        }

        if (_isPlacing)
        {
            HandlePlacementMovement();
            HandlePlacementConfirmationInput();
            HandlePlacementCancellationInput();
        }
    }

    private void HandleSelectionInput()
    {
         if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            SelectPrefabAndStartPlacing(0);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
             SelectPrefabAndStartPlacing(1);
        }
    }

    private void SelectPrefabAndStartPlacing(int index)
    {
        if (index >= 0 && index < placeablePrefabs.Count)
        {
             if (placeablePrefabs[index] != null)
             {
                _selectedPrefabIndex = index;
                string displayObjectName = $"Prefab {index+1}";

                if (index < placeableObjectNames.Count && !string.IsNullOrEmpty(placeableObjectNames[index]))
                {
                    displayObjectName = placeableObjectNames[index];
                } else {
                    displayObjectName = placeablePrefabs[index].name;
                    Debug.LogWarning($"[{nameof(ObjectPlacementManager)}]: Using prefab name for index {index} due to missing/invalid entry in placeableObjectNames list.", this);
                }

                Debug.Log($"Selected: {displayObjectName}");

                if (selectionTextDisplay != null)
                {
                    selectionTextDisplay.text = $"Selected: {displayObjectName}";
                    selectionTextDisplay.gameObject.SetActive(true);
                }

                StartPlacing();
             }
             else { Debug.LogWarning($"[{nameof(ObjectPlacementManager)}]: Prefab at index {index} is not assigned.", this); }
        }
        else { Debug.LogWarning($"[{nameof(ObjectPlacementManager)}]: Invalid prefab index: {index}. List size is {placeablePrefabs.Count}.", this); }
    }

    private void HandlePlacementMovement()
    {
         if (_currentPlacingObject == null || _mainCamera == null || Mouse.current == null) return;

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPosition);
        float currentPlacementDistance = defaultPlacementDistance;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, placementLayerMask))
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
         if (_inputManager != null && _inputManager.PlayerControls.Player.Attack.WasPerformedThisFrame())
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
        if (_selectedPrefabIndex < 0 || _selectedPrefabIndex >= placeablePrefabs.Count || placeablePrefabs[_selectedPrefabIndex] == null)
        {
            _isPlacing = false; return;
        }

        if (_currentPlacingObject != null)
        {
            Destroy(_currentPlacingObject);
            _cachedMaterials.Clear(); _originalColors.Clear();
            _currentPlacingObject = null;
        }

        _isPlacing = true;
        _currentPlacingObject = Instantiate(placeablePrefabs[_selectedPrefabIndex]);
        Debug.Log($"Started placing object: {_currentPlacingObject.name}");

        if (_currentPlacingObject.TryGetComponent<Rigidbody>(out var rb)) { rb.isKinematic = true; }
        if (_currentPlacingObject.TryGetComponent<Collider>(out var col)) { col.enabled = false; }
        ApplyPlacementTint(_currentPlacingObject);
    }

    private void ConfirmPlacement()
    {
        if (!_isPlacing || _currentPlacingObject == null) return;

        RemovePlacementTint(_currentPlacingObject);
        Debug.Log($"Placed object '{_currentPlacingObject.name}' at {_currentPlacingObject.transform.position}");

         if (_currentPlacingObject.TryGetComponent<Rigidbody>(out var rb)) { rb.isKinematic = false; }
         if (_currentPlacingObject.TryGetComponent<Collider>(out var col)) { col.enabled = true; }

        if (selectionTextDisplay != null)
        {
             selectionTextDisplay.text = "";
             selectionTextDisplay.gameObject.SetActive(false);
        }

        _currentPlacingObject = null;
        _isPlacing = false;
        _selectedPrefabIndex = -1;
    }

    private void CancelPlacing()
    {
         if (!_isPlacing || _currentPlacingObject == null) return;

        Debug.Log("Placement cancelled.");
        Destroy(_currentPlacingObject);

        _cachedMaterials.Clear(); _originalColors.Clear();

        if (selectionTextDisplay != null)
        {
            selectionTextDisplay.text = "";
            selectionTextDisplay.gameObject.SetActive(false);
        }

        _currentPlacingObject = null;
        _isPlacing = false;
        _selectedPrefabIndex = -1;
    }

    private void ApplyPlacementTint(GameObject targetObject)
    {
        if (targetObject == null) return;
        _cachedMaterials.Clear();
        _originalColors.Clear();
        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        foreach (Renderer rend in renderers) {
            Material[] materialInstances = rend.materials;
            foreach(Material matInstance in materialInstances) {
                if (matInstance != null) {
                    _cachedMaterials.Add(matInstance);
                    _originalColors.Add(matInstance.color);
                    matInstance.color = placementTint;
                }
            }
        }
    }

    private void RemovePlacementTint(GameObject targetObject)
    {
         if (_cachedMaterials.Count == 0 || _cachedMaterials.Count != _originalColors.Count) {
            _cachedMaterials.Clear();
            _originalColors.Clear();
            return;
         }
         for (int i = 0; i < _cachedMaterials.Count; i++) {
            if (_cachedMaterials[i] != null) {
                 _cachedMaterials[i].color = _originalColors[i];
            }
         }
         _cachedMaterials.Clear();
         _originalColors.Clear();
    }
}