// Scripts/Managers/GameManager.cs

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Application = UnityEngine.Device.Application;

namespace Managers
{
    public class GameManager : MonoBehaviour
    {
        public enum ControlMode
        {
            Camera,
            Menu,
            Placement,
            ObjectHolding,
            Basketball
        }

        [Header("Mode Management")] [SerializeField]
        private ControlMode currentMode = ControlMode.Camera;

        [Header("Player")] [SerializeField] private GameObject player;
        private PlayerMovement _playerMovement;
        private PlayerController _playerController;
        [SerializeField] private RoomManager roomManager;

        private UIManager _uiManager;
        private InputManager _inputManager;
        private InteractionManager _interactionManager;
        private AudioManager _audioManager;

        public static GameManager Instance { get; private set; }

        public static bool IsMobilePlatform => Application.isMobilePlatform;

        public ControlMode CurrentMode => currentMode;
        public Room CurrentRoom => roomManager.CurrentRoom;
        public MoveableObject CurrentHeldObject { get; set; }

        [FormerlySerializedAs("_mouseSensitivitySlider")] [SerializeField]
        private Slider mouseSensitivitySliderDesktop;

        [SerializeField] private Slider mouseSensitivitySliderMobile;
        [SerializeField] private Slider volumeSliderDesktop;
        [SerializeField] private Slider volumeSliderMobile;

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
            _audioManager = AudioManager.Instance;

            _playerMovement = player.GetComponent<PlayerMovement>();
            _playerController = player.GetComponent<PlayerController>();

            if (!_playerMovement || !_playerController)
            {
                enabled = false;
                return;
            }

            SetMode(ControlMode.Camera);
            _inputManager.PlayerControls.UI.Cancel.performed += OnEscapePressed;
            roomManager.DisableAllRooms();
            roomManager.MovePlayerToRoom(0);

            if (IsMobilePlatform)
            {
                if (mouseSensitivitySliderMobile)
                    mouseSensitivitySliderMobile.onValueChanged.AddListener(SetMouseSensitivity);
                if (volumeSliderMobile) volumeSliderMobile.onValueChanged.AddListener(SetVolume);
            }
            else
            {
                if (mouseSensitivitySliderDesktop)
                    mouseSensitivitySliderDesktop.onValueChanged.AddListener(SetMouseSensitivity);
                if (volumeSliderDesktop) volumeSliderDesktop.onValueChanged.AddListener(SetVolume);
            }
        }

        public void PressEscape()
        {
            switch (currentMode)
            {
                case ControlMode.Menu:
                {
                    _uiManager.CloseAllPanels();
                    SetMode(ControlMode.Camera);
                    break;
                }
                case ControlMode.Camera:
                case ControlMode.ObjectHolding:
                case ControlMode.Placement:
                default:
                {
                    _uiManager.ToggleSettingsAndControlsPanels();
                    break;
                }
            }
        }

        private void OnEscapePressed(InputAction.CallbackContext context)
        {
            switch (currentMode)
            {
                case ControlMode.Menu:
                {
                    _uiManager.CloseAllPanels();
                    SetMode(ControlMode.Camera);
                    break;
                }
                case ControlMode.Camera:
                case ControlMode.ObjectHolding:
                case ControlMode.Placement:
                default:
                {
                    _uiManager.ToggleSettingsAndControlsPanels();
                    break;
                }
            }
        }

        private void EnableObjectHoldingMode()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (_uiManager)
            {
                _uiManager.SetHoldingUI(true);
                _uiManager.ClearHint();
            }

            if (_playerMovement)
            {
                _playerMovement.enabled = true;
                _playerMovement.SetTouchLookEnabled(true);
            }
        }

        public void DropHeldObject(bool preventModeChange = false)
        {
            if (CurrentHeldObject) CurrentHeldObject.Drop(preventModeChange);
        }

        public void RotateHeldObject(float direction)
        {
            if (CurrentHeldObject) CurrentHeldObject.ApplyRotationStep(direction);
        }

        public void NudgeHeldObjectDistance(float direction)
        {
            if (CurrentHeldObject) CurrentHeldObject.AdjustDistanceStep(direction);
        }

        public void NudgeHeldObjectHorizontal(float direction)
        {
            if (CurrentHeldObject) CurrentHeldObject.ApplyHorizontalMovementStep(direction);
        }

        public void SetVolume(float volume)
        {
            if (_audioManager) _audioManager.SetMusicVolume(volume);
            if (_audioManager) _audioManager.SetSoundVolume(volume);

            if (IsMobilePlatform && volumeSliderMobile) volumeSliderMobile.value = volume;
            else if (volumeSliderDesktop) volumeSliderDesktop.value = volume;
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
            if (_playerMovement) _playerMovement.SetMouseSensitivity(sensitivity);
        }

        private void OnDestroy()
        {
            if (_inputManager) _inputManager.PlayerControls.UI.Cancel.performed -= OnEscapePressed;
        }

// In GameManager.cs, update the SetMode method:

        public void SetMode(ControlMode mode)
        {
            // If switching to a menu or placement mode from basketball mode, exit basketball.
            if ((mode == ControlMode.Menu || mode == ControlMode.Placement) && currentMode == ControlMode.Basketball)
            {
                if (BasketballManager.Instance != null && BasketballManager.Instance.IsInBasketballMode())
                {
                    BasketballManager.Instance.ExitShootingMode();
                }
            }

            currentMode = mode;
            if (_uiManager) _uiManager.OnModeChanged(mode);

            // Notify InteractionManager of mode change
            if (_interactionManager) _interactionManager.OnModeChanged(mode);

            switch (mode)
            {
                case ControlMode.Camera: EnableCameraMode(); break;
                case ControlMode.Menu: EnableMenuMode(); break;
                case ControlMode.Placement: EnablePlacementMode(); break;
                case ControlMode.ObjectHolding: EnableObjectHoldingMode(); break;
                case ControlMode.Basketball: EnableBasketballMode(); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        private void EnableCameraMode()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (_playerMovement)
            {
                _playerMovement.enabled = true;
                _playerMovement.SetTouchLookEnabled(false);
            }

            // Remove this line: if (_interactionManager) _interactionManager.enabled = true;
            if (_uiManager) _uiManager.SetHoldingUI(false);
            _inputManager.PlayerControls.Player.Enable();
        }

        private void EnableMenuMode()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (_playerMovement) _playerMovement.enabled = false;
            // Remove this line: if (_interactionManager) _interactionManager.enabled = false;
            _inputManager.PlayerControls.Player.Enable();
        }

        private void EnablePlacementMode()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (_playerMovement) _playerMovement.enabled = true;
            // Remove this line: if (_interactionManager) _interactionManager.enabled = false;
            _inputManager.PlayerControls.Player.Enable();
        }

        private void EnableBasketballMode()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (_playerMovement)
            {
                _playerMovement.enabled = true;
                _playerMovement.SetTouchLookEnabled(true);
            }

            _inputManager.PlayerControls.Player.Enable();
        }

        public void ResetCurrentRoom() => roomManager.ResetCurrentRoom();
    }
}