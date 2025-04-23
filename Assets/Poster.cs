using Interfaces;
using UnityEngine;

public class Poster : MonoBehaviour, IInteractable
{
    [SerializeField]
    private ImageUploader imageUploader;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnInteract(GameObject interactor)
    {
        imageUploader.OpenFilePicker();
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPrompt(GameObject interactor)
    {
        return "Press E to upload image";
    }
}
