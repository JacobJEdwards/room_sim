using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class PanelButton : MonoBehaviour
{
    [SerializeField]
    private TMP_Text text;
    [SerializeField]
    private Button button;

    public void SetText(string t)
    {
        text.text = t;
    }

    public void SetOnClickListener(UnityEngine.Events.UnityAction action)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}
