#nullable enable

using System.Collections.Generic;
using System.Linq;
using Interfaces;
using UnityEngine;

namespace Managers
{
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
                Debug.Log($"Interaction ray hit: {hit.collider.gameObject.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}"); // Log what was hit
                var interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    if (!interactable.CanInteract(gameObject)) return;

                    if (!_currentTargetObject)
                    {
                        _currentTarget = interactable;
                        _currentTargetObject = hit.collider.gameObject;
                        HighLightCurrentTarget();
                    }


                    _uiManager.SetHint(interactable.GetInteractionPrompt(gameObject));

                }
                else
                {
                    RestoreCurrentTarget();
                    _currentTarget = null;
                    _currentTargetObject = null;
                    _uiManager.ClearHint();
                }
            }
            else
            {
                RestoreCurrentTarget();
                _currentTarget = null;
                _currentTargetObject = null;
                _uiManager.ClearHint();
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
            if (_currentTarget == null || !_currentTargetObject) return;

            var renderer = _currentTargetObject.GetComponentInChildren<Renderer>();
            if (!renderer) return;

            _oldColors.Clear();

            foreach (var mat in renderer.materials)
            {
                _oldColors.Add(mat.color);
            }

            for (var i = 0; i < renderer.materials.Length; i++)
            {
                var mat = renderer.materials[i];
                mat.color = new Color(mat.color.r * highlightIntensity, mat.color.g * highlightIntensity,
                    mat.color.b * highlightIntensity);
            }
            renderer.materials = renderer.materials.ToArray();
        }

        private void RestoreCurrentTarget()
        {
            if (!_currentTargetObject) return;

            var renderer = _currentTargetObject.GetComponentInChildren<Renderer>();
            if (renderer)
            {
                var materials = renderer.materials;
                for (var i = 0; i < materials.Length; i++)
                {
                    materials[i].color = _oldColors[i];
                }

                renderer.materials = materials;
                _oldColors.Clear();
                _currentTargetObject = null;
                _currentTarget = null;
            }
        }
    }
}