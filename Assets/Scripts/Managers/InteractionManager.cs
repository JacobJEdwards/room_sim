// Scripts/Managers/InteractionManager.cs

#nullable enable

using System.Collections.Generic;
using Interfaces;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace Managers
{
    public class InteractionManager : MonoBehaviour
    {
        private static readonly int Color1 = Shader.PropertyToID("_Color");
        [SerializeField] private LayerMask interactionLayer;
        [SerializeField] private float interactionRange = 5f;

        private UIManager _uiManager = null!;
        private InputManager _inputManager = null!;
        private GameManager _gameManager = null!;
        private Camera? _mainCamera;
        
        private IInteractable? _currentTargetInteractable;
        private MoveableObject? _currentTargetMoveable;
        private DrawablePostIt? _currentTargetPostIt;
        private PlaceablePoster? _currentTargetPoster;
        private GameObject? _currentTargetObject;
        
        // --- ADDED ---
        private IInteractable? _lockedInteractable;
        
        private readonly List<Color> _oldColors = new();
        private readonly List<Material> _highlightedMaterials = new();
        [SerializeField] private float highlightIntensity = 1.5f;

        private const float Timeout = 0.5f;
        private float _lastInteractionTime;

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
            if (!GameManager.IsMobilePlatform)
            {
                _inputManager.PlayerControls.Player.Attack.performed += _ => OnPickupInput();
            }
        }

        private void Update()
        {
            // --- MODIFIED ---
            // If an interactable is locked, don't search for new ones
            if (_lockedInteractable != null)
            {
                if (_currentTargetObject)
                {
                    ClearCurrentTarget();
                }
                return;
            }

            // Only handle interactions when in Camera mode
            if (_gameManager.CurrentMode == GameManager.ControlMode.Camera)
            {
                HandleInteractionRaycast();
            }
            else
            {
                // Clear any existing targets when not in Camera mode
                if (_currentTargetObject)
                {
                    ClearCurrentTarget();
                }
            }
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
                    if (_currentTargetObject)
                    {
                        ClearCurrentTarget();
                    }

                    _currentTargetObject = hit.collider.gameObject;
                    _currentTargetInteractable = hit.collider.GetComponent<IInteractable>();
                    _currentTargetMoveable = hit.collider.GetComponent<MoveableObject>();
                    _currentTargetPostIt = hit.collider.GetComponent<DrawablePostIt>();
                    _currentTargetPoster = hit.collider.GetComponent<PlaceablePoster>();

                    HighLightCurrentTarget();
                    UpdateUIForTarget();
                }
            }
            else
            {
                if (_currentTargetObject)
                {
                    ClearCurrentTarget();
                }
            }
        }

        public void OnInteractInput()
        {
            if (Time.time - _lastInteractionTime < Timeout)
            {
                return;
            }
            _lastInteractionTime = Time.time;

            // --- MODIFIED ---
            // Prioritize the locked interactable, bypassing game mode checks
            if (_lockedInteractable != null)
            {
                if (_lockedInteractable.CanInteract(gameObject))
                {
                    _lockedInteractable.OnInteract(gameObject);
                }
                return;
            }
            
            // Block interactions when not in Camera mode (except Basketball mode which has its own interaction)
            if (_gameManager.CurrentMode != GameManager.ControlMode.Camera && 
                _gameManager.CurrentMode != GameManager.ControlMode.Basketball)
            {
                return;
            }

            // Special handling for Basketball mode
            if (_gameManager.CurrentMode == GameManager.ControlMode.Basketball)
            {
                // In basketball mode, 'E' exits the mode
                BasketballManager.Instance.ExitShootingMode();
                return;
            }

            if (_currentTargetInteractable != null && _currentTargetInteractable.CanInteract(gameObject))
            {
                _currentTargetInteractable.OnInteract(gameObject);
            }
        }
        
        public void OnPickupInput()
        {
            // Block pickup when not in Camera mode (except for dropping held objects)
            if (_gameManager.CurrentMode != GameManager.ControlMode.Camera && 
                _gameManager.CurrentMode != GameManager.ControlMode.ObjectHolding)
            {
                return;
            }

            if (Time.time - _lastInteractionTime < Timeout)
            {
                return;
            }

            _lastInteractionTime = Time.time;

            // If holding an object, drop it
            if (_gameManager.CurrentHeldObject)
            {
                _gameManager.CurrentHeldObject.Drop();
                return;
            }
            
            // Only allow pickup in Camera mode
            if (_gameManager.CurrentMode != GameManager.ControlMode.Camera)
            {
                return;
            }
            
            // Handle special cases for mobile
            if (GameManager.IsMobilePlatform)
            {
                if (_currentTargetPostIt != null)
                {
                    _currentTargetPostIt.ToggleMovement();
                    return;
                }
                else if (_currentTargetPoster != null)
                {
                    _currentTargetPoster.ToggleMovement();
                    return;
                }
            }
            
            if (_currentTargetMoveable)
            {
                _currentTargetMoveable.Pickup();
            }
        }

        public void OnMobileInteractPressed()
        {
            OnInteractInput();
        }
        
        public void OnMobilePickupPressed()
        {
            // Special handling for Basketball mode on mobile
            if (_gameManager.CurrentMode == GameManager.ControlMode.Basketball)
            {
                BasketballManager.Instance.ShootWithFixedForce();
                return;
            }
            
            OnPickupInput();
        }
        
        private void UpdateUIForTarget()
        {
            if (!_currentTargetObject)
            {
                _uiManager.ClearHint();
                _uiManager.ShowInteractionButtons(false, false);
                return;
            }

            bool isInteractable = _currentTargetInteractable != null;
            bool isMoveable = _currentTargetMoveable != null;
            bool isPostIt = _currentTargetPostIt != null;
            bool isPoster = _currentTargetPoster != null;
            
            string hint = "";
            if (GameManager.IsMobilePlatform)
            {
                // Mobile hints
                var hints = new List<string>();
                if (isInteractable && _currentTargetInteractable != null && _currentTargetInteractable.CanInteract(gameObject))
                {
                    hints.Add(_currentTargetInteractable.GetInteractionPromptMobile(gameObject));
                }
                if (isMoveable || isPostIt || isPoster)
                {
                    hints.Add("Tap Pickup button to pick up/move");
                }
                hint = string.Join(" | ", hints);
            }
            else
            {
                // Desktop hints  
                var hints = new List<string>();
                if (isInteractable && _currentTargetInteractable != null && _currentTargetInteractable.CanInteract(gameObject))
                {
                    hints.Add(_currentTargetInteractable.GetInteractionPromptDesktop(gameObject));
                }
                if (isMoveable)
                {
                    hints.Add("Click to pick up");
                }
                else if (isPostIt || isPoster)
                {
                    hints.Add("Click to move");
                }
                hint = string.Join(" | ", hints);
            }
            
            _uiManager.SetHint(hint);
            
            // Show appropriate buttons on mobile
            if (GameManager.IsMobilePlatform)
            {
                var showInteract = isInteractable && _currentTargetInteractable != null && _currentTargetInteractable.CanInteract(gameObject);
                var showPickup = isMoveable || isPostIt || isPoster;
                _uiManager.ShowInteractionButtons(showInteract, showPickup);
            }
        }

        private void ClearCurrentTarget()
        {
            RestoreHighlight();
            _currentTargetObject = null;
            _currentTargetInteractable = null;
            _currentTargetMoveable = null;
            _currentTargetPostIt = null;
            _currentTargetPoster = null;
            UpdateUIForTarget();
        }

        private void HighLightCurrentTarget()
        {
            if (!_currentTargetObject) return;
            var renderer = _currentTargetObject.GetComponentInChildren<Renderer>();
            if (!renderer) return;

            _oldColors.Clear();
            _highlightedMaterials.Clear();
            foreach (var mat in renderer.materials)
            {
                if (mat && mat.HasProperty(Color1))
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
                if (_highlightedMaterials[i])
                {
                    _highlightedMaterials[i].color = _oldColors[i];
                }
            }
        }
        
        private void OnDestroy()
        {
            if (_inputManager)
            {
                _inputManager.PlayerControls.Player.Interact.performed -= _ => OnInteractInput();
                if (!GameManager.IsMobilePlatform)
                {
                    _inputManager.PlayerControls.Player.Attack.performed -= _ => OnPickupInput();
                }
            }
        }

        // Add this method to be called when switching modes
        public void OnModeChanged(GameManager.ControlMode newMode)
        {
            // Clear any existing targets when mode changes
            if (_currentTargetObject && newMode != GameManager.ControlMode.Camera)
            {
                ClearCurrentTarget();
            }
        }
        
        // --- ADDED ---
        public void LockInteractable(IInteractable interactable)
        {
            _lockedInteractable = interactable;
        }

        public void UnlockInteractable()
        {
            _lockedInteractable = null;
        }
    }
}