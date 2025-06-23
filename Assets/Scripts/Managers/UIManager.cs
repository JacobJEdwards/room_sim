using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Managers
{
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
        [SerializeField] private Color cameraColor = new Color(0.2f, 0.8f, 0.4f, 0.8f);
        [SerializeField] private Color menuColor = new Color(0.2f, 0.4f, 0.8f, 0.8f);
        [SerializeField] private Color placementColor = new Color(0.8f, 0.4f, 0.2f, 0.8f);

        [Header("Panels")]
        [SerializeField] private RectTransform roomPanel;
        [SerializeField] private RectTransform inventoryPanel;
        [SerializeField] private RectTransform controlsPanel;
        [SerializeField] private float panelAnimationDuration = 0.3f;
        [SerializeField] private float panelOffscreenOffset = 400f;

        [Header("Action Buttons")]
        [SerializeField] private Button roomsButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button placementButton;

        [Header("Interaction Prompt")]
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private TMP_Text interactionText;
        [SerializeField] private CanvasGroup interactionCanvasGroup;

        // Private fields
        private TMP_Text HintText => Application.isMobilePlatform ? hintTextMobile : hintTextDesktop;
        private GameManager gameManager;
        private InputManager inputManager;
        private List<RectTransform> allPanels = new List<RectTransform>();

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
            // Get manager references
            gameManager = GameManager.Instance;
            inputManager = InputManager.Instance;

            // Setup mobile/desktop UI
            SetupPlatformSpecificUI();

            // Collect all panels
            CollectPanels();

            // Initialize panels
            InitializePanels();

            // Setup button listeners
            SetupButtons();

            // Subscribe to input
            SubscribeToInput();

            // Clear any initial hints
            ClearHint();
        }

        private void SetupPlatformSpecificUI()
        {
            if (Application.isMobilePlatform)
            {
                if (leftThumbstick != null) leftThumbstick.SetActive(true);
                if (rightThumbstick != null) rightThumbstick.SetActive(true);
                if (hintTextDesktop != null) hintTextDesktop.gameObject.SetActive(false);
                if (hintTextMobile != null) hintTextMobile.gameObject.SetActive(true);
            }
            else
            {
                if (leftThumbstick != null) leftThumbstick.SetActive(false);
                if (rightThumbstick != null) rightThumbstick.SetActive(false);
                if (hintTextDesktop != null) hintTextDesktop.gameObject.SetActive(true);
                if (hintTextMobile != null) hintTextMobile.gameObject.SetActive(false);
            }
        }

        private void CollectPanels()
        {
            allPanels.Clear();
            if (roomPanel != null) allPanels.Add(roomPanel);
            if (inventoryPanel != null) allPanels.Add(inventoryPanel);
            if (controlsPanel != null) allPanels.Add(controlsPanel);
        }

        private void InitializePanels()
        {
            // Position panels off-screen
            if (roomPanel != null)
            {
                roomPanel.anchoredPosition = new Vector2(-panelOffscreenOffset, 0);
                roomPanel.gameObject.SetActive(false);
            }

            if (inventoryPanel != null)
            {
                inventoryPanel.anchoredPosition = new Vector2(panelOffscreenOffset, 0);
                inventoryPanel.gameObject.SetActive(false);
            }

            if (controlsPanel != null)
            {
                controlsPanel.anchoredPosition = new Vector2(0, -panelOffscreenOffset);
                controlsPanel.gameObject.SetActive(true); // Always visible
            }
        }

        private void SetupButtons()
        {
            if (roomsButton != null)
                roomsButton.onClick.AddListener(ToggleRoomPanel);

            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(ToggleInventoryPanel);

            if (placementButton != null)
                placementButton.onClick.AddListener(StartPlacementMode);
        }

        private void SubscribeToInput()
        {
            if (inputManager == null) return;

            // R key for rooms
            inputManager.SetOnRKeyPressed(ToggleRoomPanel);

            // Tab for inventory (you might need to add this to InputManager)
            // inputManager.SetOnTabPressed(ToggleInventoryPanel);

            // P for placement (you might need to add this to InputManager)
            // inputManager.SetOnPKeyPressed(StartPlacementMode);
        }

        // ========== HINT SYSTEM ==========
        public void SetHint(string text)
        {
            if (HintText != null)
            {
                HintText.gameObject.SetActive(true);
                HintText.text = text;
            }

            // Also show interaction prompt if available
            ShowInteractionPrompt(text);
        }

        public void ClearHint()
        {
            if (HintText != null)
            {
                HintText.gameObject.SetActive(false);
                HintText.text = string.Empty;
            }

            // Also hide interaction prompt
            HideInteractionPrompt();
        }

        // ========== MODE INDICATOR ==========
        public void UpdateModeDisplay(GameManager.ControlMode mode)
        {
            if (modeIndicatorPanel == null || modeText == null || modeIndicatorBackground == null) 
                return;

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

        // ========== PANEL MANAGEMENT ==========
        public void ToggleRoomPanel()
        {
            if (roomPanel == null || gameManager == null) return;

            bool isActive = roomPanel.gameObject.activeSelf;
            CloseAllPanels();

            if (!isActive)
            {
                ShowPanel(roomPanel, true);
                gameManager.SetMode(GameManager.ControlMode.Menu);
            }
            else
            {
                gameManager.SetMode(GameManager.ControlMode.Camera);
            }
        }

        public void ToggleInventoryPanel()
        {
            if (inventoryPanel == null || gameManager == null) return;

            bool isActive = inventoryPanel.gameObject.activeSelf;
            CloseAllPanels();

            if (!isActive)
            {
                ShowPanel(inventoryPanel, false);
                gameManager.SetMode(GameManager.ControlMode.Menu);
            }
            else
            {
                gameManager.SetMode(GameManager.ControlMode.Camera);
            }
        }

        private void ShowPanel(RectTransform panel, bool fromLeft)
        {
            panel.gameObject.SetActive(true);
            float targetX = 0;
            panel.DOAnchorPosX(targetX, panelAnimationDuration).SetEase(Ease.OutCubic);
        }

        private void HidePanel(RectTransform panel)
        {
            if (panel == null) return;

            float targetX = panel == roomPanel ? -panelOffscreenOffset : panelOffscreenOffset;
            panel.DOAnchorPosX(targetX, panelAnimationDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(() => panel.gameObject.SetActive(false));
        }

        public void CloseAllPanels()
        {
            foreach (var panel in allPanels)
            {
                if (panel != null && panel != controlsPanel && panel.gameObject.activeSelf)
                {
                    HidePanel(panel);
                }
            }
        }

        // ========== INTERACTION PROMPT ==========
        public void ShowInteractionPrompt(string text)
        {
            if (interactionPrompt != null && interactionText != null && interactionCanvasGroup != null)
            {
                interactionPrompt.SetActive(true);
                interactionText.text = text;
                interactionCanvasGroup.DOFade(1f, 0.2f);
            }
        }

        public void HideInteractionPrompt()
        {
            if (interactionPrompt != null && interactionCanvasGroup != null)
            {
                interactionCanvasGroup.DOFade(0f, 0.2f)
                    .OnComplete(() => 
                    {
                        if (interactionPrompt != null)
                            interactionPrompt.SetActive(false);
                    });
            }
        }

        // ========== PLACEMENT MODE ==========
        private void StartPlacementMode()
        {
            CloseAllPanels();
            if (gameManager != null)
            {
                gameManager.EnterPlacementMode();
            }
        }

        // ========== UTILITY METHODS ==========
        public bool IsAnyPanelOpen()
        {
            foreach (var panel in allPanels)
            {
                if (panel != controlsPanel && panel != null && panel.gameObject.activeSelf)
                {
                    return true;
                }
            }
            return false;
        }

        public void ShowNotification(string message, float duration = 2f)
        {
            // You can implement a notification system here
            Debug.Log($"Notification: {message}");
        }

        // Called by GameManager when mode changes
        public void OnModeChanged(GameManager.ControlMode newMode)
        {
            UpdateModeDisplay(newMode);

            // Clear hints when entering menu mode
            if (newMode == GameManager.ControlMode.Menu)
            {
                ClearHint();
            }
        }
    }
}