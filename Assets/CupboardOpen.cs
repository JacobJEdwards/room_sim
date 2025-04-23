using DG.Tweening;
using Interfaces;
using UnityEngine;

public class CupboardOpen : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject doorL;
    [SerializeField] private GameObject doorR;

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
        doorL.transform.DOLocalRotate(new Vector3(0, 90f, 0), 1f);
        doorR.transform.DOLocalRotate(new Vector3(0, -90f, 0), 1f);
        open = true;
    }

    private void Close()
    {
        doorL.transform.DOLocalRotate(new Vector3(0, 0f, 0), 1f);
        doorR.transform.DOLocalRotate(new Vector3(0, 0f, 0), 1f);
        open = false;
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPrompt(GameObject interactor)
    {
        return open ? "Press E to Close Cupboard" : "Press E to Open Cupboard";
    }
}
