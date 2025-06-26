using UnityEngine;
using UnityEngine.InputSystem;

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

            // inputManager.SetOnCancelPressed(() => OnEscapePressed(default));
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
            if (currentMode == ControlMode.Placement)
            {
                SetMode(ControlMode.Camera);
            }
            else if (currentMode == ControlMode.Menu)
            {
                uiManager.CloseAllPanels();
                SetMode(ControlMode.Camera);
            }
            else
            {
                SetMode(ControlMode.Menu);
            }
        }

        public void SetMode(ControlMode mode)
        {
            currentMode = mode;

            if (uiManager != null)
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
            }
        }

        private void EnableCameraMode()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerController != null)
                playerController.enabled = true;

            if (interactionManager != null)
                interactionManager.enabled = true;

            inputManager.PlayerControls.Player.Enable();
        }

        private void EnableMenuMode()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playerController != null)
                playerController.enabled = false;

            // Disable interaction system while in menu
            if (interactionManager != null)
                interactionManager.enabled = false;

            // Player actions still enabled for ESC key
            inputManager.PlayerControls.Player.Enable();
        }

        private void EnablePlacementMode()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Enable player controller for movement
            if (playerController != null)
                playerController.enabled = true;

            // Disable normal interactions during placement
            if (interactionManager != null)
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