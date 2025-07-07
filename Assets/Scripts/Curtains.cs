using DG.Tweening;
using Interfaces;
using UnityEngine;

public class Curtains : MonoBehaviour, IInteractable
{
    private bool _open = true;
    [SerializeField]
    private float maxScale = 9.5f;
    [SerializeField] private GameObject curtain;

    public void OnInteract(GameObject interactor)
    {
        if (_open)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    private void Open()
    {
        curtain.transform.DOScaleY(1f, 1f);
        _open = true;
    }

    private void Close()
    {
        curtain.transform.DOScaleY(maxScale, 1f);
        _open = false;
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        // maybe turn to daytime ??
        return _open ? "Close Curtains" : "Open Curtains";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return _open ? "Tap to Close Curtains" : "Tap to Open Curtains";
    }

    public void ResetObject()
    {
        Open();
    }
}
