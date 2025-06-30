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

        [Header("Hint System")]
        [SerializeField] private GameObject hintPanel;
        [SerializeField] private TMP_Text hintTextDesktop;
        [SerializeField] private TMP_Text hintTextMobile;

        [Header("Mobile Controls")]
        [SerializeField] private GameObject leftThumbstick;
        [SerializeField] private GameObject rightThumbstick;
        [SerializeField] private GameObject[] toHideOnMobile;

        [Header("Mode Indicator")]
        [SerializeField] private GameObject modeIndicatorPanel;
        [SerializeField] private TMP_Text modeText;
        [SerializeField] private Image modeIndicatorBackground;
        [SerializeField] private Color cameraColor = new(0.2f, 0.8f, 0.4f, 0.8f);
        [SerializeField] private Color menuColor = new(0.2f, 0.4f, 0.8f, 0.8f);
        [SerializeField] private Color placementColor = new(0.8f, 0.4f, 0.2f, 0.8f);

        [Header("Panels To Fade (Assign in Inspector)")]
        [SerializeField] private GameObject roomPanel;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] public GameObject controlsPanel; // Now public
        [SerializeField] private GameObject placementPanel;
        [SerializeField] private float panelAnimationDuration = 0.3f;

        [Header("Action Buttons")]
        [SerializeField] private Button roomsButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button placementButton;

        [Header("Interaction Prompt")]
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private TMP_Text interactionText;
        [SerializeField] private CanvasGroup interactionCanvasGroup;

        // --- Private Fields ---
        private TMP_Text HintText => Application.isMobilePlatform ? hintTextMobile : hintTextDesktop;
        private GameManager _gameManager;
        private readonly Dictionary<GameObject, CanvasGroup> _panelCanvasGroups = new();

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
            _gameManager = GameManager.Instance;

            PreparePanelForFading(roomPanel);
            PreparePanelForFading(inventoryPanel);
            PreparePanelForFading(controlsPanel);
            PreparePanelForFading(placementPanel);

            SetupPlatformSpecificUI();
            InitializePanels();
            SetupButtons();
            ClearHint();
            if (hintPanel) hintPanel.SetActive(false);
        }

        private void Update()
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

        private void SetupButtons()
        {
            if (roomsButton) roomsButton.onClick.AddListener(ToggleRoomPanel);
            if (inventoryButton) inventoryButton.onClick.AddListener(ToggleInventoryPanel);
            if (placementButton) placementButton.onClick.AddListener(TogglePlacementPanel);
        }

        public void ToggleRoomPanel() => TogglePanel(roomPanel);
        public void TogglePlacementPanel() => TogglePanel(placementPanel);
        public void ToggleInventoryPanel() => TogglePanel(inventoryPanel);

        public void TogglePanel(GameObject panelToToggle)
        {
            if (!panelToToggle) return;

            var wasActive = panelToToggle.activeSelf;
            
            CloseAllPanels();

            if (!wasActive)
            {
                var canvasGroup = _panelCanvasGroups[panelToToggle];
                panelToToggle.SetActive(true);
                canvasGroup.DOFade(1, panelAnimationDuration);
                _gameManager?.SetMode(GameManager.ControlMode.Menu);
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
                if (panel.activeSelf)
                {
                    canvasGroup.DOFade(0, panelAnimationDuration)
                        .OnComplete(() => panel.SetActive(false));
                }
            }
        }

        public void SetHint(string text)
        {
            if (HintText)
            {
                if (hintPanel) hintPanel.SetActive(true);
                HintText.text = text;
            }
            ShowInteractionPrompt(text);
        }

        public void ClearHint()
        {
            if (HintText)
            {
                if (hintPanel) hintPanel.SetActive(false);
                HintText.text = string.Empty;
            }
            HideInteractionPrompt();
        }
        
        private void SetupPlatformSpecificUI()
        {
            if (Application.isMobilePlatform)
            {
                if (leftThumbstick) leftThumbstick.SetActive(true);
                if (rightThumbstick) rightThumbstick.SetActive(true);
                if (hintTextDesktop) hintTextDesktop.gameObject.SetActive(false);
                if (hintTextMobile) hintTextMobile.gameObject.SetActive(true);
                if (controlsPanel) controlsPanel.SetActive(false);

                foreach (var element in toHideOnMobile)
                {
                    if (element) element.SetActive(false);
                }
            }
            else
            {
                if (leftThumbstick) leftThumbstick.SetActive(false);
                if (rightThumbstick) rightThumbstick.SetActive(false);
                if (hintTextDesktop) hintTextDesktop.gameObject.SetActive(true);
                if (hintTextMobile) hintTextMobile.gameObject.SetActive(false);
            }
        }
        
        public void UpdateModeDisplay(GameManager.ControlMode mode)
        {
            if (!modeIndicatorPanel || !modeText || !modeIndicatorBackground) return;

            modeIndicatorPanel.SetActive(true);
            switch (mode)
            {
                case GameManager.ControlMode.Camera:
                    modeText.text = "Camera Mode";
                    modeIndicatorBackground.DOColor(cameraColor, 0.3f);
                    break;
                case GameManager.ControlMode.Menu:
                    modeText.text = "Menu Mode";
                    modeIndicatorBackground.DOColor(menuColor, 0.3f);
                    break;
                case GameManager.ControlMode.Placement:
                    modeText.text = "Placement Mode";
                    modeIndicatorBackground.DOColor(placementColor, 0.3f);
                    break;
            }
        }

        public void ShowInteractionPrompt(string text)
        {
            if (!interactionPrompt || !interactionText || !interactionCanvasGroup) return;
            interactionPrompt.SetActive(true);
            interactionText.text = text;
            interactionCanvasGroup.DOFade(1f, 0.2f);
        }

        public void HideInteractionPrompt()
        {
            if (interactionPrompt && interactionCanvasGroup)
            {
                interactionCanvasGroup.DOFade(0f, 0.2f).OnComplete(() =>
                {
                    if (interactionPrompt) interactionPrompt.SetActive(false);
                });
            }
        }

        public bool IsAnyPanelOpen()
        {
            return _panelCanvasGroups.Values.Any(cg => cg.alpha > 0);
        }

        public void OnModeChanged(GameManager.ControlMode newMode)
        {
            UpdateModeDisplay(newMode);
            if (newMode != GameManager.ControlMode.Menu)
            {
                CloseAllPanels();
            }
            if (newMode == GameManager.ControlMode.Menu)
            {
                ClearHint();
            }
        }
    }
}