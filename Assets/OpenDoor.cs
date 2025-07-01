using DG.Tweening;
using Interfaces;
using UnityEngine;

public class OpenDoor : MonoBehaviour, IInteractable
{

    public bool open;

    public void OnInteract(GameObject interactor)
    {
        Toggle();
    }

    public void Toggle()
    {
        if (open)
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
        transform.DOLocalRotate(new Vector3(0, 270f, 0), 1f);
        open = true;
    }

    private void Close()
    {
        transform.DOLocalRotate(new Vector3(0, 0f, 0), 1f);
        open = false;
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return open ? "Press E to close door" : "Press E to open door";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return open ? "Tap to close door" : "Tap to open door";
    }
}
