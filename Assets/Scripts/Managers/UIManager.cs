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
        [SerializeField] private GameObject desktopControlsPanel;  // This is your "menu"
        [SerializeField] private GameObject desktopPlacementPanel;
        [SerializeField] private GameObject desktopHoldingPanel;
        [SerializeField] private GameObject desktopModeIndicatorPanel;
        [SerializeField] private TMP_Text desktopModeText;
        [SerializeField] private Image desktopModeIndicatorBackground;
        [SerializeField] private GameObject desktopHintPanel;
        [SerializeField] private TMP_Text desktopHintText;
        
        // --- MOBILE UI ELEMENTS ---
        [Header("Mobile UI Elements")]
        [SerializeField] private GameObject mobileRoomPanel;
        [SerializeField] private GameObject mobileControlsPanel;  // This is your "menu"
        [SerializeField] private GameObject mobilePlacementPanel;
        [SerializeField] private GameObject mobileHoldingPanel;
        [SerializeField] private GameObject mobileModeIndicatorPanel;
        [SerializeField] private TMP_Text mobileModeText;
        [SerializeField] private Image mobileModeIndicatorBackground;
        [SerializeField] private GameObject mobileHintPanel;
        [SerializeField] private TMP_Text mobileHintText;
        [SerializeField] private GameObject leftThumbstick;
        [SerializeField] private GameObject rightThumbstick;

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
        private GameObject _activeControlsPanel;
        private GameObject _activePlacementPanel;
        private GameObject _activeHoldingPanel;
        private GameObject _activeModeIndicatorPanel;
        private TMP_Text _activeModeText;
        private Image _activeModeIndicatorBackground;
        private GameObject _activeHintPanel;
        private TMP_Text _activeHintText;

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
                return;
            }
            
            // Determine platform once
            _isMobilePlatform = Application.isMobilePlatform;
            
            // Setup UI immediately in Awake so it's ready for other scripts
            SetupPlatformSpecificUI();
            PreparePanels();
            InitializePanels();
        }

        private void Start()
        {
            _gameManager = GameManager.Instance;
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
                _activeControlsPanel = mobileControlsPanel;
                _activePlacementPanel = mobilePlacementPanel;
                _activeHoldingPanel = mobileHoldingPanel;
                _activeModeIndicatorPanel = mobileModeIndicatorPanel;
                _activeModeText = mobileModeText;
                _activeModeIndicatorBackground = mobileModeIndicatorBackground;
                _activeHintPanel = mobileHintPanel;
                _activeHintText = mobileHintText;
                
                // Log panel assignments for debugging
                Debug.Log($"Mobile panels assigned - Room: {_activeRoomPanel?.name ?? "NULL"}, Controls: {_activeControlsPanel?.name ?? "NULL"}, Placement: {_activePlacementPanel?.name ?? "NULL"}");
                
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
                _activeControlsPanel = desktopControlsPanel;
                _activePlacementPanel = desktopPlacementPanel;
                _activeHoldingPanel = desktopHoldingPanel;
                _activeModeIndicatorPanel = desktopModeIndicatorPanel;
                _activeModeText = desktopModeText;
                _activeModeIndicatorBackground = desktopModeIndicatorBackground;
                _activeHintPanel = desktopHintPanel;
                _activeHintText = desktopHintText;
            }
            
            // Hide hint panel initially
            if (_activeHintPanel) _activeHintPanel.SetActive(false);
        }

        private void PreparePanels()
        {
            // Prepare all active panels for animation
            if (_activeRoomPanel) PreparePanelForFading(_activeRoomPanel);
            if (_activeControlsPanel) PreparePanelForFading(_activeControlsPanel);
            if (_activePlacementPanel) PreparePanelForFading(_activePlacementPanel);
            if (_activeHoldingPanel) PreparePanelForFading(_activeHoldingPanel);
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

        public void ToggleRoomPanel()
        {
            Debug.Log($"ToggleRoomPanel called. Current panel active: {_activeRoomPanel?.activeSelf}");
            if (_activeRoomPanel == null)
            {
                Debug.LogError("Room panel is null! Check the mobileRoomPanel assignment in the Inspector.");
                return;
            }
            TogglePanel(_activeRoomPanel);
        }
        
        public void TogglePlacementPanel()
        {
            Debug.Log($"TogglePlacementPanel called. Current panel active: {_activePlacementPanel?.activeSelf}");
            if (_activePlacementPanel == null)
            {
                Debug.LogError("Placement panel is null! Check the mobilePlacementPanel assignment in the Inspector.");
                return;
            }
            TogglePanel(_activePlacementPanel);
        }
        
        public void ToggleControlsPanel()
        {
            Debug.Log($"ToggleControlsPanel called. Current panel active: {_activeControlsPanel?.activeSelf}");
            if (_activeControlsPanel == null)
            {
                Debug.LogError("Controls panel is null! Check the mobileControlsPanel assignment in the Inspector.");
                return;
            }
            TogglePanel(_activeControlsPanel);
        }

        // Remove ToggleInventoryPanel since you don't have inventory

        private void TogglePanel(GameObject panelToToggle)
        {
            if (!panelToToggle)
            {
                Debug.LogError("Panel to toggle is null!");
                return;
            }

            Debug.Log($"TogglePanel: {panelToToggle.name}, currently active: {panelToToggle.activeSelf}");
            Debug.Log($"Call Stack: {System.Environment.StackTrace}");

            var wasActive = panelToToggle.activeSelf;
            
            CloseAllPanels();

            if (wasActive)
            {
                _gameManager?.SetMode(GameManager.ControlMode.Camera);
            }
            else
            {
                if (!_panelCanvasGroups.TryGetValue(panelToToggle, out var canvasGroup)) 
                {
                    Debug.LogError($"Panel {panelToToggle.name} not found in canvas groups!");
                    return;
                }

                panelToToggle.SetActive(true);
                canvasGroup.DOFade(1, panelAnimationDuration);
                _gameManager?.SetMode(GameManager.ControlMode.Menu);
            }
        }

        public void CloseAllPanels()
        {
            foreach (var (panel, canvasGroup) in _panelCanvasGroups)
            {
                if (panel && panel.activeSelf && panel != _activeHoldingPanel)
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