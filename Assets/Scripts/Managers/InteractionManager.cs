#nullable enable

using System.Collections.Generic;
using System.Linq;
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

        private IInteractable? _currentTarget;
        private InputManager _inputManager = null!;
        private GameObject? _currentTargetObject;

        private Camera? _mainCamera;

        private readonly List<Color> _oldColors = new();
        private readonly List<Material> _highlightedMaterials = new(); // Keep track of materials we've changed
        [SerializeField]
        private float highlightIntensity = 1.5f;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            _uiManager = UIManager.Instance;
            _inputManager = InputManager.Instance;
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

            if (Physics.Raycast(ray, out var hit, interactionRange, interactionLayer))
            {
                var interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    if (!interactable.CanInteract(gameObject))
                    {
                        RestoreCurrentTarget(); // Restore if we can't interact
                        return;
                    }

                    // Check if we are already highlighting this object
                    if (_currentTargetObject != hit.collider.gameObject)
                    {
                        RestoreCurrentTarget(); // Restore the old one
                        _currentTarget = interactable;
                        _currentTargetObject = hit.collider.gameObject;
                        HighLightCurrentTarget();
                    }

                    var prompt = Application.isMobilePlatform
                        ? interactable.GetInteractionPromptMobile(gameObject)
                        : interactable.GetInteractionPromptDesktop(gameObject);

                    _uiManager.SetHint(prompt);
                }
                else
                {
                    RestoreCurrentTarget();
                }
            }
            else
            {
                RestoreCurrentTarget();
            }
        }

        private void OnInteractInput()
        {
            if (_currentTarget != null && _currentTarget.CanInteract(gameObject))
            {
                _currentTarget.OnInteract(gameObject);
            }
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
                // --- THIS IS THE FIX ---
                // Check if the material has a "_Color" property before trying to access it.
                if (mat != null && mat.HasProperty("_Color"))
                {
                    _highlightedMaterials.Add(mat);
                    _oldColors.Add(mat.color);
                    mat.color = new Color(mat.color.r * highlightIntensity, mat.color.g * highlightIntensity, mat.color.b * highlightIntensity);
                }
            }
        }

        private void RestoreCurrentTarget()
        {
            if (_currentTargetObject == null) return;

            // Only restore colors if we have saved data
            if (_highlightedMaterials.Count > 0)
            {
                 for (var i = 0; i < _highlightedMaterials.Count; i++)
                {
                    if (_highlightedMaterials[i] != null)
                    {
                        _highlightedMaterials[i].color = _oldColors[i];
                    }
                }
            }
           
            // Clear the lists and references
            _highlightedMaterials.Clear();
            _oldColors.Clear();
            _currentTargetObject = null;
            _currentTarget = null;
            if(_uiManager) _uiManager.ClearHint();
        }
    }
}