using DG.Tweening;
using Interfaces;
using UnityEngine;

public class OpenCloseToilet : MonoBehaviour, IInteractable
{
    private bool _open;

    [SerializeField]
    private GameObject seat;

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
        seat.transform.DOLocalRotate(new Vector3(0, 0, 90f), 1f);
        _open = true;
    }

    private void Close()
    {
        seat.transform.DOLocalRotate(new Vector3(0, 0f, 0), 1f);
        _open = false;
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return _open ? "Close Toilet" : "Open Toilet";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return _open ? "Tap to Close Toilet" : "Tap to Open Toilet";
    }
}
