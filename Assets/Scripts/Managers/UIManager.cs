// Scripts/Managers/UIManager.cs

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;

namespace Managers
{
    using Application = UnityEngine.Device.Application;

    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; } = null!;

        // --- CANVAS REFERENCES ---
        [Header("Canvas Management")]
        [Tooltip("The main canvas containing desktop-specific UI elements")]
        [SerializeField] private GameObject desktopCanvas;
        
        [Tooltip("The main canvas containing mobile-specific UI elements")]
        [SerializeField] private GameObject mobileCanvas;

        // --- DESKTOP UI ELEMENTS ---
        [Header("Desktop UI Elements")]
        [SerializeField] private GameObject desktopRoomPanel;
        [SerializeField] private GameObject desktopInventoryPanel;
        [SerializeField] private GameObject desktopControlsPanel;
        [SerializeField] private GameObject desktopPlacementPanel;
        [SerializeField] private GameObject desktopHoldingPanel;
        [SerializeField] private GameObject desktopModeIndicatorPanel;
        [SerializeField] private TMP_Text desktopModeText;
        [SerializeField] private Image desktopModeIndicatorBackground;
        [SerializeField] private GameObject desktopHintPanel;
        [SerializeField] private TMP_Text desktopHintText;
        [SerializeField] private Button desktopRoomsButton;
        [SerializeField] private Button desktopInventoryButton;
        [SerializeField] private Button desktopPlacementButton;
        
        // --- MOBILE UI ELEMENTS ---
        [Header("Mobile UI Elements")]
        [SerializeField] private GameObject mobileRoomPanel;
        [SerializeField] private GameObject mobileInventoryPanel;
        [SerializeField] private GameObject mobileControlsPanel;
        [SerializeField] private GameObject mobilePlacementPanel;
        [SerializeField] private GameObject mobileHoldingPanel;
        [SerializeField] private GameObject mobileModeIndicatorPanel;
        [SerializeField] private TMP_Text mobileModeText;
        [SerializeField] private Image mobileModeIndicatorBackground;
        [SerializeField] private GameObject mobileHintPanel;
        [SerializeField] private TMP_Text mobileHintText;
        [SerializeField] private GameObject leftThumbstick;
        [SerializeField] private GameObject rightThumbstick;
        [SerializeField] private Button mobileRoomsButton;
        [SerializeField] private Button mobileInventoryButton;
        [SerializeField] private Button mobilePlacementButton;

        // --- MODE COLORS ---
        [Header("Mode Indicator Colors")]
        [SerializeField] private Color cameraColor = new(0.2f, 0.8f, 0.4f, 0.8f);
        [SerializeField] private Color menuColor = new(0.2f, 0.4f, 0.8f, 0.8f);
        [SerializeField] private Color placementColor = new(0.8f, 0.4f, 0.2f, 0.8f);

        // --- ANIMATION SETTINGS ---
        [Header("Animation")]
        [SerializeField] private float panelAnimationDuration = 0.3f;

        // --- Private Fields ---
        private GameManager _gameManager;
        private readonly Dictionary<GameObject, CanvasGroup> _panelCanvasGroups = new();
        private bool _isMobilePlatform;
        
        // Active references based on platform
        private GameObject _activeRoomPanel;
        private GameObject _activeInventoryPanel;
        private GameObject _activeControlsPanel;
        private GameObject _activePlacementPanel;
        private GameObject _activeHoldingPanel;
        private GameObject _activeModeIndicatorPanel;
        private TMP_Text _activeModeText;
        private Image _activeModeIndicatorBackground;
        private GameObject _activeHintPanel;
        private TMP_Text _activeHintText;
        private Button _activeRoomsButton;
        private Button _activeInventoryButton;
        private Button _activePlacementButton;

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
            
            // Determine platform once
            _isMobilePlatform = Application.isMobilePlatform;
        }

        private void Start()
        {
            _gameManager = GameManager.Instance;
            
            // Setup platform-specific UI
            SetupPlatformSpecificUI();
            
            // Prepare panels for animation
            PreparePanels();
            
            // Initialize panels
            InitializePanels();
            
            // Setup button listeners
            SetupButtons();
        }

        private void SetupPlatformSpecificUI()
        {
            if (_isMobilePlatform)
            {
                // Activate mobile canvas, deactivate desktop
                if (mobileCanvas) mobileCanvas.SetActive(true);
                if (desktopCanvas) desktopCanvas.SetActive(false);
                
                // Set active references to mobile elements
                _activeRoomPanel = mobileRoomPanel;
                _activeInventoryPanel = mobileInventoryPanel;
                _activeControlsPanel = mobileControlsPanel;
                _activePlacementPanel = mobilePlacementPanel;
                _activeHoldingPanel = mobileHoldingPanel;
                _activeModeIndicatorPanel = mobileModeIndicatorPanel;
                _activeModeText = mobileModeText;
                _activeModeIndicatorBackground = mobileModeIndicatorBackground;
                _activeHintPanel = mobileHintPanel;
                _activeHintText = mobileHintText;
                _activeRoomsButton = mobileRoomsButton;
                _activeInventoryButton = mobileInventoryButton;
                _activePlacementButton = mobilePlacementButton;
                
                // Ensure mobile controls are visible
                if (leftThumbstick) leftThumbstick.SetActive(true);
                if (rightThumbstick) rightThumbstick.SetActive(true);
            }
            else
            {
                // Activate desktop canvas, deactivate mobile
                if (desktopCanvas) desktopCanvas.SetActive(true);
                if (mobileCanvas) mobileCanvas.SetActive(false);
                
                // Set active references to desktop elements
                _activeRoomPanel = desktopRoomPanel;
                _activeInventoryPanel = desktopInventoryPanel;
                _activeControlsPanel = desktopControlsPanel;
                _activePlacementPanel = desktopPlacementPanel;
                _activeHoldingPanel = desktopHoldingPanel;
                _activeModeIndicatorPanel = desktopModeIndicatorPanel;
                _activeModeText = desktopModeText;
                _activeModeIndicatorBackground = desktopModeIndicatorBackground;
                _activeHintPanel = desktopHintPanel;
                _activeHintText = desktopHintText;
                _activeRoomsButton = desktopRoomsButton;
                _activeInventoryButton = desktopInventoryButton;
                _activePlacementButton = desktopPlacementButton;
            }
            
            // Hide hint panel initially
            if (_activeHintPanel) _activeHintPanel.SetActive(false);
        }

        private void PreparePanels()
        {
            // Prepare all desktop panels for animation
            if (!_isMobilePlatform)
            {
                PreparePanelForFading(desktopRoomPanel);
                PreparePanelForFading(desktopInventoryPanel);
                PreparePanelForFading(desktopControlsPanel);
                PreparePanelForFading(desktopPlacementPanel);
                PreparePanelForFading(desktopHoldingPanel);
            }
            // Prepare all mobile panels for animation
            else
            {
                PreparePanelForFading(mobileRoomPanel);
                PreparePanelForFading(mobileInventoryPanel);
                PreparePanelForFading(mobileControlsPanel);
                PreparePanelForFading(mobilePlacementPanel);
                PreparePanelForFading(mobileHoldingPanel);
            }
        }

        private void PreparePanelForFading(GameObject panel)
        {
            if (!panel || _panelCanvasGroups.ContainsKey(panel)) return;
            
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (!canvasGroup) canvasGroup = panel.AddComponent<CanvasGroup>();
            
            _panelCanvasGroups.Add(panel, canvasGroup);
        }

        private void InitializePanels()
        {
            foreach (var (panel, canvasGroup) in _panelCanvasGroups)
            {
                canvasGroup.alpha = 0;
                panel.SetActive(false);
            }
        }

        private void SetupButtons()
        {
            // Setup platform-specific buttons
            if (_activeRoomsButton) _activeRoomsButton.onClick.AddListener(ToggleRoomPanel);
            if (_activeInventoryButton) _activeInventoryButton.onClick.AddListener(ToggleInventoryPanel);
            if (_activePlacementButton) _activePlacementButton.onClick.AddListener(TogglePlacementPanel);
        }

        // --- HINT SYSTEM ---
        public void SetHint(string text)
        {
            if (_activeHintPanel && _activeHintText)
            {
                _activeHintPanel.SetActive(true);
                _activeHintText.text = text;
            }
        }

        public void ClearHint()
        {
            if (_activeHintPanel) _activeHintPanel.SetActive(false);
        }

        // --- MODE MANAGEMENT ---
        public void OnModeChanged(GameManager.ControlMode newMode)
        {
            UpdateModeDisplay(newMode);
            
            if (newMode != GameManager.ControlMode.Menu)
            {
                CloseAllPanels();
            }
            
            if (newMode is GameManager.ControlMode.Menu or GameManager.ControlMode.Placement)
            {
                ClearHint();
            }
        }

        private void UpdateModeDisplay(GameManager.ControlMode mode)
        {
            if (!_activeModeIndicatorPanel || !_activeModeText || !_activeModeIndicatorBackground) return;

            _activeModeIndicatorPanel.SetActive(true);
            
            switch (mode)
            {
                case GameManager.ControlMode.Camera:
                    _activeModeText.text = "Camera Mode";
                    _activeModeIndicatorBackground.DOColor(cameraColor, 0.3f);
                    break;
                case GameManager.ControlMode.Menu:
                    _activeModeText.text = "Menu Mode";
                    _activeModeIndicatorBackground.DOColor(menuColor, 0.3f);
                    break;
                case GameManager.ControlMode.Placement:
                    _activeModeText.text = "Placement Mode";
                    _activeModeIndicatorBackground.DOColor(placementColor, 0.3f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        // --- PANEL MANAGEMENT ---
        private void Update()
        {
            // Keyboard shortcuts (desktop only)
            if (!_isMobilePlatform)
            {
                if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    ToggleRoomPanel();
                }

                if (Keyboard.current.pKey.wasPressedThisFrame)
                {
                    TogglePlacementPanel();
                }
            }
        }

        public void ToggleRoomPanel() => TogglePanel(_activeRoomPanel);
        public void TogglePlacementPanel() => TogglePanel(_activePlacementPanel);
        public void ToggleInventoryPanel() => TogglePanel(_activeInventoryPanel);
        public void ToggleControlsPanel() => TogglePanel(_activeControlsPanel);

        public void TogglePanel(GameObject panelToToggle)
        {
            if (!panelToToggle) return;

            var wasActive = panelToToggle.activeSelf;
            
            CloseAllPanels();

            if (wasActive)
            {
                _gameManager?.SetMode(GameManager.ControlMode.Camera);
            }
            else
            {
                if (!_panelCanvasGroups.TryGetValue(panelToToggle, out var canvasGroup)) return;

                panelToToggle.SetActive(true);
                canvasGroup.DOFade(1, panelAnimationDuration);
                _gameManager?.SetMode(GameManager.ControlMode.Menu);
            }
        }

        public void CloseAllPanels()
        {
            foreach (var (panel, canvasGroup) in _panelCanvasGroups)
            {
                if (panel.activeSelf && panel != _activeHoldingPanel)
                {
                    canvasGroup.DOFade(0, panelAnimationDuration)
                        .OnComplete(() => panel.SetActive(false));
                }
            }
        }

        public void OpenSettingsPanel()
        {
            ToggleControlsPanel();
        }

        public void ShowHoldingPanel()
        {
            if (!_activeHoldingPanel || !_panelCanvasGroups.TryGetValue(_activeHoldingPanel, out var canvasGroup)) return;
            
            _activeHoldingPanel.SetActive(true);
            canvasGroup.DOFade(1, panelAnimationDuration);
        }

        public void HideHoldingPanel()
        {
            if (_activeHoldingPanel && _panelCanvasGroups.TryGetValue(_activeHoldingPanel, out var canvasGroup))
            {
                canvasGroup.DOFade(0, panelAnimationDuration)
                    .OnComplete(() => _activeHoldingPanel.SetActive(false));
            }
        }

        public bool IsAnyPanelOpen()
        {
            return _panelCanvasGroups.Values.Any(cg => cg.alpha > 0);
        }

        // --- PUBLIC GETTERS ---
        public bool IsMobilePlatform => _isMobilePlatform;
        
        public GameObject GetActiveCanvas()
        {
            return _isMobilePlatform ? mobileCanvas : desktopCanvas;
        }
    }
}