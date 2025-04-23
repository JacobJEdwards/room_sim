using DG.Tweening;
using Interfaces;
using UnityEngine;

public class OpenDoor : MonoBehaviour, IInteractable
{

    public bool open;

    public void OnInteract(GameObject interactor)
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

    public string GetInteractionPrompt(GameObject interactor)
    {
        return open ? "Press E to Close Door" : "Press E to Open Door";
    }
}
