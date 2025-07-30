// Scripts/PlaceablePoster.cs

using Interfaces;
using UnityEngine;

[RequireComponent(typeof(ImageUploader))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(MoveableObject))]
public class PlaceablePoster : MonoBehaviour, IInteractable, IHasName
{
    public string Name => "Poster";

    [Header("Interaction Prompts")] [SerializeField]
    private string changeImagePromptDesktop = "Press E to change image";

    [SerializeField] private string changeImagePromptMobile = "Tap to change image";

    private ImageUploader _imageUploader;
    private Renderer _renderer;
    private MoveableObject _moveableObject;

    private void Awake()
    {
        _imageUploader = GetComponent<ImageUploader>();
        _renderer = GetComponent<Renderer>();
        _moveableObject = GetComponent<MoveableObject>();

        if (!_renderer)
        {
            Debug.LogError("A Renderer component is required on this object.", this);
            enabled = false;
            return;
        }

        if (_imageUploader != null)
        {
            _imageUploader.OnImageUploaded.AddListener(UpdateTexture);
        }
    }

    private void OnDestroy()
    {
        if (_imageUploader != null)
        {
            _imageUploader.OnImageUploaded.RemoveListener(UpdateTexture);
        }
    }

    public void ToggleMovement()
    {
        if (_moveableObject.IsHeld)
        {
            _moveableObject.Drop();
        }
        else
        {
            _moveableObject.Pickup();
        }
    }

    public void OnInteract(GameObject interactor)
    {
        _imageUploader.OpenFilePicker();
    }

    public bool CanInteract(GameObject interactor)
    {
        return !_moveableObject.IsHeld;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        if (_moveableObject.IsHeld) return "Moving... (Click to place)";
        return changeImagePromptDesktop;
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        if (_moveableObject.IsHeld) return "Moving... (Tap Pickup button to place)";
        return changeImagePromptMobile;
    }

    public void UpdateTexture(Texture2D newTexture)
    {
        if (_renderer && _renderer.material)
        {
            Debug.Log(
                $"Updating texture on {gameObject.name}. New texture size: {newTexture.width}x{newTexture.height}");
            _renderer.material.mainTexture = newTexture;
        }
        else
        {
            Debug.LogError("Cannot update texture: Renderer or material is null");
        }
    }

    public void ResetObject()
    {
        if (_moveableObject.IsHeld)
        {
            _moveableObject.Drop();
        }

        _moveableObject.ResetObject();
    }
}