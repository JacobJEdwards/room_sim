
using Interfaces;
using UnityEngine;

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

        _imageUploader.OnImageUploaded.AddListener(UpdateTexture);
    }

    private void UpdateTexture(Texture2D newTexture)
    {
        if (_renderer && _renderer.material)
        {
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
        if (_imageUploader)
        {
            _imageUploader.OnImageUploaded.RemoveListener(UpdateTexture);
        }
    }

    public void ResetObject()
    {
        if (_renderer && _renderer.material)
        {
            _renderer.material.mainTexture = null;
        }
    }
}