using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Canvas Management")]
        [SerializeField] private GameObject desktopCanvas;
        [SerializeField] private GameObject mobileCanvas;

        [Header("Desktop UI Elements")]
        [SerializeField] private GameObject desktopRoomPanel;
        [SerializeField] private GameObject desktopControlsPanel;
        [SerializeField] private GameObject desktopPlacementPanel;
        [SerializeField] private GameObject desktopHoldingPanel;
        [SerializeField] private GameObject desktopModeIndicatorPanel;
        [SerializeField] private TMP_Text desktopModeText;
        [SerializeField] private Image desktopModeIndicatorBackground;
        [SerializeField] private GameObject desktopHintPanel;
        [SerializeField] private TMP_Text desktopHintText;

        [Header("Mobile UI Elements")]
        [SerializeField] private GameObject mobileRoomPanel;
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
        [SerializeField] private GameObject mobileInteractButton;
        [SerializeField] private GameObject mobilePickupButton;
        [SerializeField] private GameObject mobileHoldingControlsPanel;

        [Header("Mode Indicator Colors")]
        [SerializeField] private Color cameraColor = new(0.2f, 0.8f, 0.4f, 0.8f);
        [SerializeField] private Color menuColor = new(0.2f, 0.4f, 0.8f, 0.8f);
        [SerializeField] private Color placementColor = new(0.8f, 0.4f, 0.2f, 0.8f);

        [Header("Animation")]
        [SerializeField] private float panelAnimationDuration = 0.3f;

        private GameManager _gameManager;
        private InteractionManager _interactionManager;
        private readonly Dictionary<GameObject, CanvasGroup> _panelCanvasGroups = new();
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

            SetupPlatformSpecificUI();
            PreparePanels();
            InitializePanels();
        }

        private void Start()
        {
            _gameManager = GameManager.Instance;
            _interactionManager = FindFirstObjectByType<InteractionManager>();

            if (_gameManager.IsMobilePlatform)
            {
                ConnectMobileButtons();
            }
        }

        private void ConnectMobileButtons()
        {
            if (_interactionManager == null)
            {
                return;
            }

            if (mobileInteractButton != null)
            {
                Button interactBtn = mobileInteractButton.GetComponent<Button>();
                if (interactBtn != null)
                {
                    interactBtn.onClick.RemoveAllListeners();
                    interactBtn.onClick.AddListener(() => _interactionManager.OnMobileInteractPressed());
                }
            }

            if (mobilePickupButton != null)
            {
                Button pickupBtn = mobilePickupButton.GetComponent<Button>();
                if (pickupBtn != null)
                {
                    pickupBtn.onClick.RemoveAllListeners();
                    pickupBtn.onClick.AddListener(() => _interactionManager.OnMobilePickupPressed());
                }
            }
        }

        private void Update()
        {
            if (GameManager.Instance.IsMobilePlatform) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                ToggleRoomPanel();
            }

            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                ToggleObjectPlacementMenu();
            }
        }

        private void SetupPlatformSpecificUI()
        {
            if (GameManager.Instance.IsMobilePlatform)
            {
                if (mobileCanvas) mobileCanvas.SetActive(true);
                if (desktopCanvas) desktopCanvas.SetActive(false);
                _activeRoomPanel = mobileRoomPanel;
                _activeControlsPanel = mobileControlsPanel;
                _activePlacementPanel = mobilePlacementPanel;
                _activeHoldingPanel = mobileHoldingPanel;
                _activeModeIndicatorPanel = mobileModeIndicatorPanel;
                _activeModeText = mobileModeText;
                _activeModeIndicatorBackground = mobileModeIndicatorBackground;
                _activeHintPanel = mobileHintPanel;
                _activeHintText = mobileHintText;
            }
            else
            {
                if (desktopCanvas) desktopCanvas.SetActive(true);
                if (mobileCanvas) mobileCanvas.SetActive(false);
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
            if (_activeHintPanel) _activeHintPanel.SetActive(false);
        }

        private void PreparePanels()
        {
            if (_activeRoomPanel) PreparePanelForFading(_activeRoomPanel);
            if (_activeControlsPanel) PreparePanelForFading(_activeControlsPanel);
            if (_activePlacementPanel) PreparePanelForFading(_activePlacementPanel);
            if (_activeHoldingPanel) PreparePanelForFading(_activeHoldingPanel);
        }

        private void PreparePanelForFading(GameObject panel)
        {
            if (!panel || _panelCanvasGroups.ContainsKey(panel)) return;
            var canvasGroup = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
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

        public void SetHint(string text)
        {
            if (_activeHintPanel && _activeHintText)
            {
                _activeHintPanel.SetActive(!string.IsNullOrEmpty(text));
                _activeHintText.text = text;
            }
        }

        public void ClearHint() => SetHint("");

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
            string modeName;
            Color modeColor;
            switch (mode)
            {
                case GameManager.ControlMode.Camera: modeName = "Camera Mode"; modeColor = cameraColor; break;
                case GameManager.ControlMode.Menu: modeName = "Menu Mode"; modeColor = menuColor; break;
                case GameManager.ControlMode.Placement: modeName = "Placement Mode"; modeColor = placementColor; break;
                case GameManager.ControlMode.ObjectHolding:
                default:
                    _activeModeIndicatorPanel.SetActive(false);
                    return;
            }
            _activeModeText.text = modeName;
            _activeModeIndicatorBackground.DOColor(modeColor, 0.3f);
        }

        public void ToggleRoomPanel() => TogglePanel(_activeRoomPanel);
        public void ToggleControlsPanel() => TogglePanel(_activeControlsPanel);
        public void ToggleObjectPlacementMenu() => TogglePanel(_activePlacementPanel);

        private void TogglePanel(GameObject panelToToggle)
        {
            if (!panelToToggle) return;
            var wasActive = panelToToggle.activeSelf;
            CloseAllPanels();
            if (!wasActive)
            {
                if (_panelCanvasGroups.TryGetValue(panelToToggle, out var canvasGroup))
                {
                    panelToToggle.SetActive(true);
                    canvasGroup.DOFade(1, panelAnimationDuration);
                    _gameManager?.SetMode(GameManager.ControlMode.Menu);
                }
            }
            else
            {
                _gameManager?.SetMode(GameManager.ControlMode.Camera);
            }
        }

        public void CloseAllPanels()
        {
            foreach (var (panel, canvasGroup) in _panelCanvasGroups)
            {
                if (panel && panel.activeSelf && panel != _activeHoldingPanel)
                {
                    canvasGroup.DOFade(0, panelAnimationDuration).OnComplete(() => panel.SetActive(false));
                }
            }
        }

        public void ShowInteractionButtons(bool showInteract, bool showPickup)
        {
            if (!GameManager.Instance.IsMobilePlatform) return;
            if (mobileInteractButton) mobileInteractButton.SetActive(showInteract);
            if (mobilePickupButton)
            {
                mobilePickupButton.SetActive(showPickup);
                if (showPickup)
                {
                    var buttonText = mobilePickupButton.GetComponentInChildren<TMP_Text>();
                    if (buttonText)
                        buttonText.text = "Pickup";
                }
            }
        }

        public void SetHoldingUI(bool isHolding)
        {
            if (GameManager.Instance.IsMobilePlatform)
            {
                if (mobileHoldingControlsPanel) mobileHoldingControlsPanel.SetActive(isHolding);
                if (mobileInteractButton) mobileInteractButton.SetActive(!isHolding);
                if (leftThumbstick) leftThumbstick.SetActive(!isHolding);
                if (rightThumbstick) rightThumbstick.SetActive(!isHolding);
                if (isHolding && mobilePickupButton)
                {
                    mobilePickupButton.SetActive(true);
                    var buttonText = mobilePickupButton.GetComponentInChildren<TMP_Text>();
                    if (buttonText)
                        buttonText.text = "Drop";
                }
            }
            else
            {
                if (_activeHoldingPanel)
                {
                    _activeHoldingPanel.SetActive(isHolding);
                    if (_panelCanvasGroups.TryGetValue(_activeHoldingPanel, out var canvasGroup))
                    {
                        canvasGroup.alpha = isHolding ? 1 : 0;
                    }
                }
            }
        }

        public void SetPlacementModeUI(bool isActive)
        {
            if (_activeHoldingPanel)
            {
                _activeHoldingPanel.SetActive(isActive);
                if (_panelCanvasGroups.TryGetValue(_activeHoldingPanel, out var canvasGroup))
                {
                    canvasGroup.alpha = isActive ? 1 : 0;
                }
            }
        }

        public bool IsAnyPanelOpen() => _panelCanvasGroups.Values.Any(cg => cg.alpha > 0);
        
        private void OnDestroy()
        {
            if (GameManager.Instance.IsMobilePlatform)
            {
                if (mobileInteractButton != null)
                {
                    Button interactBtn = mobileInteractButton.GetComponent<Button>();
                    if (interactBtn != null)
                        interactBtn.onClick.RemoveAllListeners();
                }

                if (mobilePickupButton != null)
                {
                    Button pickupBtn = mobilePickupButton.GetComponent<Button>();
                    if (pickupBtn != null)
                        pickupBtn.onClick.RemoveAllListeners();
                }
            }
        }
    }
}