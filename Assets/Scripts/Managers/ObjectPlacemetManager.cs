#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Interfaces;
using UnityEngine.Assertions;
using Application = UnityEngine.Device.Application;

namespace Managers
{
    public class ObjectPlacementManager : MonoBehaviour
    {
        private static readonly int Color1 = Shader.PropertyToID("_Color");
        public GameManager gameManager = null!;
        private UIManager _uiManager = null!;

        [Header("Placement Settings")]
        [SerializeField]
        [Tooltip("The list of prefabs that can be instantiated and placed.")]
        private List<GameObject> placeablePrefabs = new();

        [SerializeField] [Tooltip("The layer(s) the object can be placed upon.")]
        private LayerMask placementLayerMask;

        [SerializeField] [Tooltip("How far from the camera the object floats when not over a valid surface.")]
        private float defaultPlacementDistance = 3f;

        [Header("Visuals")] [SerializeField] [Tooltip("Color tint to apply while placing the object.")]
        private Color placementTint = new(1f, 0.5f, 0.5f, 0.75f);

        private InputManager? _inputManager;
        private Camera? _mainCamera;

        private GameObject? _currentPlacingObject;
        private int _selectedPrefabIndex = -1;
        private bool _isPlacingNonMoveable;

        private readonly List<Material> _cachedMaterials = new();
        private readonly List<Color> _originalColors = new();

        [SerializeField] private GameObject objectButtonPanelMobile = null!;
        [SerializeField] private GameObject objectButtonPanelDesktop = null!;

        [SerializeField] private PanelButton objectButtonPrefab = null!;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            gameManager = GameManager.Instance;
            _inputManager = InputManager.Instance;
            _uiManager = UIManager.Instance;

            Assert.IsNotNull(gameManager);
            Assert.IsNotNull(_inputManager);
            Assert.IsNotNull(_uiManager);

            InitialiseObjects();
        }

        private void InitialiseObjects()
        {
            var panel = Application.isMobilePlatform ? objectButtonPanelMobile : objectButtonPanelDesktop;
            for (var i = 0; i < placeablePrefabs.Count; i++)
            {
                var btn = Instantiate(objectButtonPrefab, panel.transform);
                Assert.IsNotNull(btn);

                var prefab = placeablePrefabs[i];
                Assert.IsNotNull(prefab);
                btn.SetText(prefab.TryGetComponent<IHasName>(out var hasName) ? hasName.Name : $"Object {i + 1}");

                var index = i;
                btn.SetOnClickListener(() => SelectPrefabAndStartPlacing(index));
            }
        }

        private void Update()
        {
            if (!_isPlacingNonMoveable) return;

            HandleNonMoveablePlacement();
            HandlePlacementConfirmationInput();
            HandlePlacementCancellationInput();
        }

        private float _lastSpawnTime;
        private const float SpawnCooldown = 0.5f;

        public void PlaceRandomPrefab()
        {
            if (placeablePrefabs.Count == 0) return;
            var randomIndex = Random.Range(0, placeablePrefabs.Count);
            SelectPrefabAndStartPlacing(randomIndex);
        }

        public void SelectPrefabAndStartPlacing(int index)
        {
            if (Time.time - _lastSpawnTime < SpawnCooldown)
            {
                return;
            }

            _lastSpawnTime = Time.time;

            if (index < 0 || index >= placeablePrefabs.Count || !placeablePrefabs[index])
            {
                return;
            }

            var curRoom = gameManager.CurrentRoom;
            _uiManager.CloseAllPanels();
            var prefabToPlace = placeablePrefabs[index];
            var spawnPos = _mainCamera!.transform.position + (_mainCamera.transform.forward * 2f);
            var newObject = Instantiate(prefabToPlace, spawnPos, Quaternion.identity, curRoom.transform);
            curRoom.AddPlacedObject(newObject);

            if (newObject.TryGetComponent<MoveableObject>(out var moveable))
            {
                moveable.Pickup(isNewlySpawned: true);
            }
            else if (newObject.TryGetComponent<DrawablePostIt>(out var postIt))
            {
                var allPostIts = newObject.GetComponentsInChildren<DrawablePostIt>();
                postIt.ToggleMovement();
                gameManager.SetMode(GameManager.ControlMode.Camera);
            }
            else if (newObject.TryGetComponent<PlaceablePoster>(out var poster))
            {
                var allPosters = newObject.GetComponentsInChildren<PlaceablePoster>();
                poster.ToggleMovement();
                gameManager.SetMode(GameManager.ControlMode.Camera);
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

            gameManager.SetMode(GameManager.ControlMode.Placement);
            ApplyPlacementTint(_currentPlacingObject);
            _uiManager.SetHint("Click to place / Right-click to cancel");
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

            gameManager.CurrentRoom.AddPlacedObject(_currentPlacingObject);
            _uiManager.ClearHint();

            _currentPlacingObject = null;
            _isPlacingNonMoveable = false;
            _selectedPrefabIndex = -1;

            gameManager.SetMode(GameManager.ControlMode.Camera);
        }

        private void CancelNonMoveablePlacement()
        {
            if (!_currentPlacingObject) return;

            Destroy(_currentPlacingObject);
            _uiManager.ClearHint();

            _currentPlacingObject = null;
            _isPlacingNonMoveable = false;
            _selectedPrefabIndex = -1;

            gameManager.SetMode(GameManager.ControlMode.Camera);
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