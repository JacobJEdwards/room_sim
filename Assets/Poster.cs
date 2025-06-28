// Scripts/Poster.cs

using Interfaces;
using UnityEngine;

// This ensures the ImageUploader is always on the same object
[RequireComponent(typeof(ImageUploader))]
public class Poster : MonoBehaviour, IInteractable
{
    private ImageUploader _imageUploader;
    private Renderer _renderer;

    private void Awake()
    {
        _imageUploader = GetComponent<ImageUploader>();
        _renderer = GetComponent<Renderer>();

        if (!_renderer)
        {
            Debug.LogError("Poster script requires a Renderer component on the same GameObject.", this);
            enabled = false;
            return;
        }

        // --- THIS IS THE KEY CHANGE ---
        // We directly subscribe to the event in the code.
        // This removes the need to set it up in the Inspector.
        _imageUploader.OnImageUploaded.AddListener(UpdateTexture);
    }

    // This method is called by the ImageUploader when the image is ready
    public void UpdateTexture(Texture2D newTexture)
    {
        if (_renderer && _renderer.material)
        {
            // Apply the new texture to the material
            _renderer.material.mainTexture = newTexture;
        }
    }

    public void OnInteract(GameObject interactor)
    {
        _imageUploader.OpenFilePicker();
    }

    public bool CanInteract(GameObject interactor) => true;

    public string GetInteractionPromptMobile(GameObject interactor) => "Tap to change poster";

    public string GetInteractionPromptDesktop(GameObject interactor) => "Press E to change poster";

    private void OnDestroy()
    {
        // Good practice to unsubscribe from events when the object is destroyed
        if (_imageUploader != null)
        {
            _imageUploader.OnImageUploaded.RemoveListener(UpdateTexture);
        }
    }
}