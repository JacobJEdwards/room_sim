using DG.Tweening;
using Interfaces;
using UnityEngine;

public class Curtains : MonoBehaviour, IInteractable
{
    private bool _open = true;
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
        curtain.transform.DOScaleY(9.5f, 1f);
        _open = false;
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPrompt(GameObject interactor)
    {
        // maybe turn to daytime ??
        return _open ? "Press E to Close Curtains" : "Press E to Open Curtains";
    }
}
