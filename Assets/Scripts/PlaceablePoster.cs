
using Interfaces;
using UnityEngine;

[RequireComponent(typeof(ImageUploader))]
public class PlaceablePoster : MonoBehaviour, IInteractable
{
    private ImageUploader _imageUploader;
    private Renderer _renderer;

    private void Awake()
    {
        _imageUploader = GetComponent<ImageUploader>();
        _renderer = GetComponent<Renderer>();

        if (!_renderer)
        {
            Debug.LogError("A Renderer component is required on this object.", this);
            enabled = false;
            return;
        }

        // Subscribe to the image upload event
        if (_imageUploader != null)
        {
            _imageUploader.OnImageUploaded.AddListener(UpdateTexture);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event to prevent memory leaks
        if (_imageUploader != null)
        {
            _imageUploader.OnImageUploaded.RemoveListener(UpdateTexture);
        }
    }

    public void OnInteract(GameObject interactor)
    {
        // When the poster is interacted with, open the file picker.
        _imageUploader.OpenFilePicker();
    }

    public bool CanInteract(GameObject interactor)
    {
        // You can add logic here to determine if the player is allowed to interact.
        return true;
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return "Tap to change poster";
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return "Press E to change poster";
    }

    // This method will be called by the ImageUploader when the image is ready.
    public void UpdateTexture(Texture2D newTexture)
    {
        if (_renderer && _renderer.material)
        {
            Debug.Log($"Updating texture on {gameObject.name}. New texture size: {newTexture.width}x{newTexture.height}");
            _renderer.material.mainTexture = newTexture;
        }
        else
        {
            Debug.LogError("Cannot update texture: Renderer or material is null");
        }
    }
}