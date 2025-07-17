using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Application = UnityEngine.Application;

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

        [Header("Mode Management")] [SerializeField]
        private ControlMode currentMode = ControlMode.Camera;

        [Header("Player")] [SerializeField] private GameObject player;
        [SerializeField] private MonoBehaviour playerController;
        private PlayerController _playerController;
        [SerializeField] private RoomManager roomManager;

        // Managers
        private UIManager _uiManager;
        private InputManager _inputManager;
        private InteractionManager _interactionManager;

        public static GameManager Instance { get; private set; }

        public ControlMode CurrentMode => currentMode;
        public Room CurrentRoom => roomManager.CurrentRoom;

        [FormerlySerializedAs("_mouseSensitivitySlider")] [SerializeField]
        private Slider mouseSensitivitySlider;

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _uiManager = UIManager.Instance;
            _inputManager = InputManager.Instance;
            _interactionManager = FindFirstObjectByType<InteractionManager>();

            SetMode(ControlMode.Camera);

            _inputManager.PlayerControls.UI.Cancel.performed += OnEscapePressed;

            roomManager.DisableAllRooms();
            roomManager.MovePlayerToRoom(0);

            _playerController = player.GetComponent<PlayerController>();
            if (!_playerController)
            {
                Debug.LogError("PlayerController component not found on player GameObject.", this);
                enabled = false;
                return;
            }
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
            if (_playerController)
            {
                _playerController.SetMouseSensitivity(sensitivity);
            }
        }

        private void OnDestroy()
        {
            if (_inputManager)
            {
                _inputManager.PlayerControls.UI.Cancel.performed -= OnEscapePressed;
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
                    _uiManager.CloseAllPanels();
                    SetMode(ControlMode.Camera);
                    break;
                case ControlMode.Camera:
                default:
                    // This is the line that was changed
                    _uiManager.ToggleControlsPanel();
                    break;
            }
        }

        public void SetMode(ControlMode mode)
        {
            currentMode = mode;

            if (_uiManager)
            {
                _uiManager.OnModeChanged(mode);
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

            if (_interactionManager)
                _interactionManager.enabled = true;

            _inputManager.PlayerControls.Player.Enable();
        }

        private void EnableMenuMode()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playerController)
                playerController.enabled = false;

            if (_interactionManager)
                _interactionManager.enabled = false;

            _inputManager.PlayerControls.Player.Enable();
        }

        private void EnablePlacementMode()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerController)
                playerController.enabled = true;

            if (_interactionManager)
                _interactionManager.enabled = false;

            _inputManager.PlayerControls.Player.Enable();
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
            return currentMode is ControlMode.Camera or ControlMode.Placement;
        }

        public bool ShouldProcessUIInput()
        {
            return currentMode == ControlMode.Menu;
        }

        public void ResetCurrentRoom()
        {
            roomManager.ResetCurrentRoom();
        }
    }
}