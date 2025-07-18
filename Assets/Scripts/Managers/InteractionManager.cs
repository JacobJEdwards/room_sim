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
            
            // Add click handler for desktop pickup (but not on mobile)
            if (!_gameManager.IsMobilePlatform)
            {
                _inputManager.PlayerControls.Player.Attack.performed += _ => OnPickupInput();
            }
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
            // E key or Interact button - always for interaction only
            if (_currentTargetInteractable != null && _currentTargetInteractable.CanInteract(gameObject))
            {
                _currentTargetInteractable.OnInteract(gameObject);
            }
        }
        
        public void OnPickupInput()
        {
            // Click or Pickup button - for picking up/dropping objects
            if (_gameManager.CurrentHeldObject != null)
            {
                _gameManager.CurrentHeldObject.Drop();
                return;
            }
            
            if (_currentTargetMoveable != null)
            {
                _currentTargetMoveable.Pickup();
            }
        }
        
        // Mobile-specific methods to be called by UI buttons
        public void OnMobileInteractPressed()
        {
            OnInteractInput();
        }
        
        public void OnMobilePickupPressed()
        {
            OnPickupInput();
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

            // Build hint text based on capabilities
            string hint = "";
            if (_gameManager.IsMobilePlatform)
            {
                // Mobile hints
                List<string> hints = new List<string>();
                if (isInteractable && _currentTargetInteractable.CanInteract(gameObject))
                {
                    hints.Add(_currentTargetInteractable.GetInteractionPromptMobile(gameObject));
                }
                if (isMoveable)
                {
                    hints.Add("Tap Pickup button to pick up");
                }
                hint = string.Join(" | ", hints);
            }
            else
            {
                // Desktop hints  
                List<string> hints = new List<string>();
                if (isInteractable && _currentTargetInteractable.CanInteract(gameObject))
                {
                    hints.Add(_currentTargetInteractable.GetInteractionPromptDesktop(gameObject));
                }
                if (isMoveable)
                {
                    hints.Add("Click to pick up");
                }
                hint = string.Join(" | ", hints);
            }
            
            _uiManager.SetHint(hint);
            
            // Show appropriate buttons on mobile
            if (_gameManager.IsMobilePlatform)
            {
                bool showInteract = isInteractable && _currentTargetInteractable.CanInteract(gameObject);
                bool showPickup = isMoveable;
                _uiManager.ShowInteractionButtons(showInteract, showPickup);
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
        
        private void OnDestroy()
        {
            if (_inputManager != null)
            {
                _inputManager.PlayerControls.Player.Interact.performed -= _ => OnInteractInput();
                if (!_gameManager.IsMobilePlatform)
                {
                    _inputManager.PlayerControls.Player.Attack.performed -= _ => OnPickupInput();
                }
            }
        }
    }
}