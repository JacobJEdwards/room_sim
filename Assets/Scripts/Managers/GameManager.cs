// Scripts/Managers/GameManager.cs

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Device;
using Application = UnityEngine.Application; // Required for Application.isMobilePlatform

namespace Managers
{
    public class GameManager : MonoBehaviour
    {
        public enum ControlMode
        {
            Camera,
            Menu,
            Placement
        }

        [Header("Mode Management")]
        [SerializeField] private ControlMode currentMode = ControlMode.Camera;

        [Header("Player")]
        [SerializeField] private GameObject player;
        [SerializeField] private MonoBehaviour playerController;

        // Managers
        private UIManager uiManager;
        private InputManager inputManager;
        private InteractionManager interactionManager;

        private static GameManager instance;
        public static GameManager Instance => instance;
        public ControlMode CurrentMode => currentMode;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            uiManager = UIManager.Instance;
            inputManager = InputManager.Instance;
            interactionManager = FindObjectOfType<InteractionManager>();

            SetMode(ControlMode.Camera);

            inputManager.PlayerControls.UI.Cancel.performed += OnEscapePressed;
        }

        private void OnDestroy()
        {
            if (inputManager != null)
            {
                inputManager.PlayerControls.UI.Cancel.performed -= OnEscapePressed;
            }
        }

        private void OnEscapePressed(InputAction.CallbackContext context)
        {
            switch (currentMode)
            {
                case ControlMode.Placement:
                    SetMode(ControlMode.Camera);
                    break;
                case ControlMode.Menu:
                    uiManager.CloseAllPanels();
                    SetMode(ControlMode.Camera);
                    break;
                case ControlMode.Camera:
                default:
                {
                    SetMode(ControlMode.Menu);
                    if (uiManager && !Application.isMobilePlatform)
                    {
                        uiManager.TogglePanel(uiManager.controlsPanel);
                    }

                    break;
                }
            }
        }

        public void SetMode(ControlMode mode)
        {
            currentMode = mode;

            if (uiManager)
            {
                uiManager.OnModeChanged(mode);
            }

            switch (mode)
            {
                case ControlMode.Camera:
                    EnableCameraMode();
                    break;

                case ControlMode.Menu:
                    EnableMenuMode();
                    break;

                case ControlMode.Placement:
                    EnablePlacementMode();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        private void EnableCameraMode()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerController)
                playerController.enabled = true;

            if (interactionManager)
                interactionManager.enabled = true;

            inputManager.PlayerControls.Player.Enable();
        }

        private void EnableMenuMode()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playerController)
                playerController.enabled = false;

            if (interactionManager)
                interactionManager.enabled = false;

            inputManager.PlayerControls.Player.Enable();
        }

        private void EnablePlacementMode()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerController)
                playerController.enabled = true;

            if (interactionManager)
                interactionManager.enabled = false;

            inputManager.PlayerControls.Player.Enable();
        }

        public void ToggleMenuMode()
        {
            SetMode(currentMode == ControlMode.Menu ? ControlMode.Camera : ControlMode.Menu);
        }

        public void EnterPlacementMode()
        {
            SetMode(ControlMode.Placement);
        }

        public void ExitPlacementMode()
        {
            SetMode(ControlMode.Camera);
        }

        public bool ShouldProcessPlayerInput()
        {
            return currentMode == ControlMode.Camera || currentMode == ControlMode.Placement;
        }

        public bool ShouldProcessUIInput()
        {
            return currentMode == ControlMode.Menu;
        }
    }
}