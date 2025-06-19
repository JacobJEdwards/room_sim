using System;
using TMPro;
using UnityEngine;

namespace Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; } = null!;

        [SerializeField] private TMP_Text hintTextDesktop;
        [SerializeField] private TMP_Text hintTextMobile;

        [SerializeField] private GameObject leftThumbstick;
        [SerializeField] private GameObject rightThumbstick;

        private TMP_Text HintText =>
            Application.isMobilePlatform ? hintTextMobile : hintTextDesktop;

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
            if (Application.isMobilePlatform)
            {
                leftThumbstick.SetActive(true);
                rightThumbstick.SetActive(true);
                hintTextDesktop.gameObject.SetActive(false);
                hintTextMobile.gameObject.SetActive(true);
            }
            else
            {
                leftThumbstick.SetActive(false);
                rightThumbstick.SetActive(false);
                hintTextDesktop.gameObject.SetActive(true);
                hintTextMobile.gameObject.SetActive(false);
            }

            ClearHint();
        }

        public void SetHint(string text)
        {
            HintText.gameObject.SetActive(true);
            HintText.text = text;
        }

        public void ClearHint()
        {
            HintText.gameObject.SetActive(false);
            HintText.text = string.Empty;
        }

    }
}