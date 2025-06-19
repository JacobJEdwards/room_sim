using Interfaces;
using UnityEngine;

public class Poster : MonoBehaviour, IInteractable
{
    [SerializeField]
    private ImageUploader imageUploader;

    public void OnInteract(GameObject interactor)
    {
        imageUploader.OpenFilePicker();
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return "Press E to upload image";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return "Tap to upload image";
    }
}
