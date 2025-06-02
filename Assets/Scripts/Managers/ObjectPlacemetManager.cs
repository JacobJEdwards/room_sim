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

    [Header("UI")]
    [SerializeField]
    [Tooltip("Assign the panel that contains the placeable object buttons.")]
    private GameObject? placementPanel;


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

        if (placementPanel != null)
        {
            placementPanel.SetActive(false);
        } else {
             Debug.LogWarning($"[{nameof(ObjectPlacementManager)}]: Placement Panel is not assigned in the Inspector. UI will not function.", this);
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

        if (_isPlacing)
        {
            HandlePlacementMovement();
            HandlePlacementConfirmationInput();
            HandlePlacementCancellationInput();
        }
    }

    public void ShowPlacementPanel()
    {
        if (placementPanel != null)
        {
            bool isPanelBeingOpened = !placementPanel.activeSelf;
            placementPanel.SetActive(isPanelBeingOpened);

            if (isPanelBeingOpened)
            {
                // Show and unlock cursor to use the UI panel
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (!_isPlacing) // Only lock if we are just closing the panel, not starting placement
            {
                // Hide and lock cursor when returning to gameplay
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public void SelectPrefabAndStartPlacing(int index)
    {
        if (index >= 0 && index < placeablePrefabs.Count)
        {
             if (placeablePrefabs[index] != null)
             {
                _selectedPrefabIndex = index;
                Debug.Log($"Selected: {placeablePrefabs[index].name}");

                if (placementPanel != null)
                {
                    placementPanel.SetActive(false);
                }

                // --- MODIFIED BEHAVIOR ---
                // Deactivate mouse immediately after clicking the button in the panel.
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                
                StartPlacing();
             }
             else { Debug.LogWarning($"[{nameof(ObjectPlacementManager)}]: Prefab at index {index} is not assigned.", this); }
        }
        else { Debug.LogWarning($"[{nameof(ObjectPlacementManager)}]: Invalid prefab index: {index}. List size is {placeablePrefabs.Count}.", this); }
    }


    private void HandlePlacementMovement()
    {
         if (_currentPlacingObject == null || _mainCamera == null || Mouse.current == null) return;

        // With a locked cursor, the "mouse position" is effectively the center of the screen.
        // This will now cast a ray from the center of the camera's view.
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = _mainCamera.ScreenPointToRay(screenCenter);
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

        _currentPlacingObject = null;
        _isPlacing = false;
        _selectedPrefabIndex = -1;

        // Ensure cursor is locked (it should be already, but this is safe)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void CancelPlacing()
    {
         if (!_isPlacing || _currentPlacingObject == null) return;

        Debug.Log("Placement cancelled.");
        Destroy(_currentPlacingObject);

        _cachedMaterials.Clear(); _originalColors.Clear();

        if (placementPanel != null)
        {
            placementPanel.SetActive(false);
        }

        _currentPlacingObject = null;
        _isPlacing = false;
        _selectedPrefabIndex = -1;

        // Ensure cursor is locked
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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