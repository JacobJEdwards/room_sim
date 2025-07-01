using DG.Tweening;
using Interfaces;
using UnityEngine;

public class DrawPull : MonoBehaviour, IInteractable
{
    [SerializeField] private float to = 0.5f;
    private bool open;

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
        transform.DOLocalMoveX(to, 1f);
        open = true;
    }

    private void Close()
    {
        transform.DOLocalMoveX(0, 1f);
        open = false;
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return open ? "Press E to close drawer" : "Press E to open drawer";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return open ? "Tap to close drawer" : "Tap to open drawer";
        
    }
}
