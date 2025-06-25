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
        [SerializeField] private TMP_Text hintTextDesktop;
        [SerializeField] private TMP_Text hintTextMobile;

        [Header("Mobile Controls")]
        [SerializeField] private GameObject leftThumbstick;
        [SerializeField] private GameObject rightThumbstick;

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
        [SerializeField] private GameObject controlsPanel; // Assign if you want this to be managed
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
        // This dictionary will hold the CanvasGroup for each panel, which is needed for fading.
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

            // Prepare all panels for fading
            PreparePanelForFading(roomPanel);
            PreparePanelForFading(inventoryPanel);
            PreparePanelForFading(placementPanel);
            // You can also prepare the controlsPanel if you want to fade it
            // PreparePanelForFading(controlsPanel); 

            SetupPlatformSpecificUI();
            InitializePanels();
            SetupButtons();
            ClearHint();
        }

        private void Update()
        {
            // Direct key checks for simplicity and reliability
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                ToggleRoomPanel();
            }

            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                TogglePlacementPanel();
            }
        }

        // Helper method to get or add a CanvasGroup to a panel
        private void PreparePanelForFading(GameObject panel)
        {
            if (panel != null && !_panelCanvasGroups.ContainsKey(panel))
            {
                var canvasGroup = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
                _panelCanvasGroups.Add(panel, canvasGroup);
            }
        }

        private void InitializePanels()
        {
            // Set the initial state for all managed panels
            foreach (var entry in _panelCanvasGroups)
            {
                var panel = entry.Key;
                var canvasGroup = entry.Value;
                canvasGroup.alpha = 0; // Start fully transparent
                panel.SetActive(false); // Start inactive
            }
        }

        private void SetupButtons()
        {
            if (roomsButton) roomsButton.onClick.AddListener(ToggleRoomPanel);
            if (inventoryButton) inventoryButton.onClick.AddListener(ToggleInventoryPanel);
            if (placementButton) placementButton.onClick.AddListener(TogglePlacementPanel);
        }

        // --- Public Toggle Methods ---
        public void ToggleRoomPanel() => TogglePanel(roomPanel);
        public void TogglePlacementPanel() => TogglePanel(placementPanel);
        public void ToggleInventoryPanel() => TogglePanel(inventoryPanel);

        // --- Core Panel Logic ---
        private void TogglePanel(GameObject panelToToggle)
        {
            if (panelToToggle == null) return;

            var wasActive = panelToToggle.activeSelf;
            
            // Always close any currently open panel first
            CloseAllPanels();

            // If the panel was closed, open it now.
            if (!wasActive)
            {
                var canvasGroup = _panelCanvasGroups[panelToToggle];
                panelToToggle.SetActive(true);
                canvasGroup.DOFade(1, panelAnimationDuration);
                _gameManager?.SetMode(GameManager.ControlMode.Menu);
            }
            else
            {
                // If it was already open, CloseAllPanels handled it. Just set the mode.
                _gameManager?.SetMode(GameManager.ControlMode.Camera);
            }
        }

        public void CloseAllPanels()
        {
            foreach (var entry in _panelCanvasGroups)
            {
                var panel = entry.Key;
                if (panel.activeSelf)
                {
                    var canvasGroup = entry.Value;
                    canvasGroup.DOFade(0, panelAnimationDuration)
                        .OnComplete(() => panel.SetActive(false));
                }
            }
        }

        // --- Hint System and Other UI Methods ---
        public void SetHint(string text)
        {
            if (HintText)
            {
                HintText.gameObject.SetActive(true);
                HintText.text = text;
            }
            ShowInteractionPrompt(text);
        }

        public void ClearHint()
        {
            if (HintText)
            {
                HintText.gameObject.SetActive(false);
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
            // A panel is open if its CanvasGroup is visible.
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