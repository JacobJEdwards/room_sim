using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
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
        [SerializeField] private Color cameraColor = new (0.2f, 0.8f, 0.4f, 0.8f);
        [SerializeField] private Color menuColor = new (0.2f, 0.4f, 0.8f, 0.8f);
        [SerializeField] private Color placementColor = new (0.8f, 0.4f, 0.2f, 0.8f);

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
        private GameManager _gameManager;
        private InputManager _inputManager;
        private readonly List<RectTransform> _allPanels = new ();

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
            _gameManager = GameManager.Instance;
            _inputManager = InputManager.Instance;

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

        private void CollectPanels()
        {
            _allPanels.Clear();
            if (roomPanel) _allPanels.Add(roomPanel);
            if (inventoryPanel) _allPanels.Add(inventoryPanel);
            if (controlsPanel) _allPanels.Add(controlsPanel);
        }

        private void InitializePanels()
        {
            // Position panels off-screen
            if (roomPanel)
            {
                roomPanel.anchoredPosition = new Vector2(-panelOffscreenOffset, 0);
                roomPanel.gameObject.SetActive(false);
            }

            if (inventoryPanel)
            {
                inventoryPanel.anchoredPosition = new Vector2(panelOffscreenOffset, 0);
                inventoryPanel.gameObject.SetActive(false);
            }

            if (controlsPanel)
            {
                controlsPanel.anchoredPosition = new Vector2(0, -panelOffscreenOffset);
                controlsPanel.gameObject.SetActive(true);
            }
        }

        private void SetupButtons()
        {
            if (roomsButton)
                roomsButton.onClick.AddListener(ToggleRoomPanel);

            if (inventoryButton)
                inventoryButton.onClick.AddListener(ToggleInventoryPanel);

            if (placementButton)
                placementButton.onClick.AddListener(StartPlacementMode);
        }

        private void SubscribeToInput()
        {
            if (!_inputManager) return;

            // R key for rooms
            _inputManager.SetOnRKeyPressed(ToggleRoomPanel);

            // inputManager.SetOnTabPressed(ToggleInventoryPanel);

            // inputManager.SetOnPKeyPressed(StartPlacementMode);
        }

        // ========== HINT SYSTEM ==========
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

        // ========== MODE INDICATOR ==========
        public void UpdateModeDisplay(GameManager.ControlMode mode)
        {
            if (!modeIndicatorPanel || !modeText || !modeIndicatorBackground)
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
            if (!roomPanel || _gameManager == null) return;

            var isActive = roomPanel.gameObject.activeSelf;
            CloseAllPanels();

            if (!isActive)
            {
                ShowPanel(roomPanel);
                _gameManager.SetMode(GameManager.ControlMode.Menu);
            }
            else
            {
                _gameManager.SetMode(GameManager.ControlMode.Camera);
            }
        }

        public void ToggleInventoryPanel()
        {
            if (!inventoryPanel || _gameManager == null) return;

            var isActive = inventoryPanel.gameObject.activeSelf;
            CloseAllPanels();

            if (!isActive)
            {
                ShowPanel(inventoryPanel);
                _gameManager.SetMode(GameManager.ControlMode.Menu);
            }
            else
            {
                _gameManager.SetMode(GameManager.ControlMode.Camera);
            }
        }

        private void ShowPanel(RectTransform panel)
        {
            panel.gameObject.SetActive(true);
            const float targetX = 0;
            panel.DOAnchorPosX(targetX, panelAnimationDuration).SetEase(Ease.OutCubic);
        }

        private void HidePanel(RectTransform panel)
        {
            if (!panel) return;

            var targetX = panel == roomPanel ? -panelOffscreenOffset : panelOffscreenOffset;
            panel.DOAnchorPosX(targetX, panelAnimationDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(() => panel.gameObject.SetActive(false));
        }

        public void CloseAllPanels()
        {
            foreach (var panel in _allPanels.Where(panel => panel && panel != controlsPanel && panel.gameObject.activeSelf))
            {
                HidePanel(panel);
            }
        }

        // ========== INTERACTION PROMPT ==========
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
                interactionCanvasGroup.DOFade(0f, 0.2f)
                    .OnComplete(() => 
                    {
                        if (interactionPrompt)
                            interactionPrompt.SetActive(false);
                    });
            }
        }

        // ========== PLACEMENT MODE ==========
        private void StartPlacementMode()
        {
            CloseAllPanels();
            if (_gameManager != null)
            {
                _gameManager.EnterPlacementMode();
            }
        }

        // ========== UTILITY METHODS ==========
        public bool IsAnyPanelOpen()
        {
            return _allPanels.Any(panel => panel != controlsPanel && panel && panel.gameObject.activeSelf);
        }

        public void ShowNotification(string message, float duration = 2f)
        {
            Debug.Log($"Notification: {message}");
        }

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