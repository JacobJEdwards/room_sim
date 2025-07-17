#nullable enable

using System.Collections.Generic;
using Interfaces;
using UnityEngine;

namespace Managers
{
    using Application = UnityEngine.Device.Application;

    public class InteractionManager : MonoBehaviour
    {
        [SerializeField] private LayerMask interactionLayer;
        [SerializeField] private float interactionRange = 5f;

        private UIManager _uiManager = null!;
        private InputManager _inputManager = null!;
        private GameManager _gameManager = null!;
        private Camera? _mainCamera;
        
        private IInteractable? _currentTargetInteractable;
        private MoveableObject? _currentTargetMoveable;
        private GameObject? _currentTargetObject;
        
        private readonly List<Color> _oldColors = new();
        private readonly List<Material> _highlightedMaterials = new();
        [SerializeField] private float highlightIntensity = 1.5f;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            _uiManager = UIManager.Instance;
            _inputManager = InputManager.Instance;
            _gameManager = GameManager.Instance;
            interactionLayer = LayerMask.GetMask("Interaction");
            
            _inputManager.PlayerControls.Player.Interact.performed += _ => OnInteractInput();
        }

        private void Update()
        {
            HandleInteractionRaycast();
        }

        private void HandleInteractionRaycast()
        {
            if (!_mainCamera || !_uiManager) return;

            var ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            var hasHit = Physics.Raycast(ray, out var hit, interactionRange, interactionLayer);

            if (hasHit)
            {
                if (_currentTargetObject != hit.collider.gameObject)
                {
                    if (_currentTargetObject != null)
                    {
                        ClearCurrentTarget();
                    }

                    _currentTargetObject = hit.collider.gameObject;
                    _currentTargetInteractable = hit.collider.GetComponent<IInteractable>();
                    _currentTargetMoveable = hit.collider.GetComponent<MoveableObject>();

                    HighLightCurrentTarget();
                    UpdateUIForTarget();
                }
            }
            else
            {
                if (_currentTargetObject != null)
                {
                    ClearCurrentTarget();
                }
            }
        }

        public void OnInteractInput()
        {
            if (_gameManager.CurrentHeldObject != null)
            {
                _gameManager.CurrentHeldObject.Drop();
                return;
            }
            
            if (_currentTargetInteractable != null && _currentTargetInteractable.CanInteract(gameObject))
            {
                _currentTargetInteractable.OnInteract(gameObject);
            }
            else if (_currentTargetMoveable != null)
            {
                _currentTargetMoveable.Pickup();
            }
        }
        
        private void UpdateUIForTarget()
        {
            if (_currentTargetObject == null)
            {
                _uiManager.ClearHint();
                _uiManager.ShowInteractionButtons(false, false);
                return;
            }

            bool isInteractable = _currentTargetInteractable != null;
            bool isMoveable = _currentTargetMoveable != null;

            string hint = "";
            if (isInteractable)
            {
                hint = _uiManager.IsMobilePlatform
                    ? _currentTargetInteractable.GetInteractionPromptMobile(gameObject)
                    : _currentTargetInteractable.GetInteractionPromptDesktop(gameObject);
            }
            else if (isMoveable)
            {
                 hint = "Press E to pick up";
            }
            _uiManager.SetHint(hint);
            
            if (_uiManager.IsMobilePlatform)
            {
                _uiManager.ShowInteractionButtons(isInteractable || isMoveable, false);
            }
        }

        private void ClearCurrentTarget()
        {
            RestoreHighlight();
            _currentTargetObject = null;
            _currentTargetInteractable = null;
            _currentTargetMoveable = null;
            UpdateUIForTarget();
        }

        private void HighLightCurrentTarget()
        {
            if (_currentTargetObject == null) return;
            var renderer = _currentTargetObject.GetComponentInChildren<Renderer>();
            if (!renderer) return;

            _oldColors.Clear();
            _highlightedMaterials.Clear();
            foreach (var mat in renderer.materials)
            {
                if (mat != null && mat.HasProperty("_Color"))
                {
                    _highlightedMaterials.Add(mat);
                    _oldColors.Add(mat.color);
                    mat.color *= highlightIntensity;
                }
            }
        }

        private void RestoreHighlight()
        {
            if (_highlightedMaterials.Count == 0) return;
            for (var i = 0; i < _highlightedMaterials.Count; i++)
            {
                if (_highlightedMaterials[i] != null)
                {
                    _highlightedMaterials[i].color = _oldColors[i];
                }
            }
        }
    }
}