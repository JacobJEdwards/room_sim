using TMPro;
using UnityEngine;

namespace Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; } = null!;

        [SerializeField] private TMP_Text hintTextDesktop;
        [SerializeField] private TMP_Text hintTextMobile;

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