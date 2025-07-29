// Scripts/ImageUploader.cs

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

public class ImageUploader : MonoBehaviour
{
    // This event will be invoked when an image is successfully uploaded.
    public UnityEvent<Texture2D> OnImageUploaded;

#if UNITY_EDITOR
    // This field will only be visible in the Unity Editor
    [Header("Editor Testing")]
    [Tooltip("Assign a texture here to simulate an upload in the Editor.")]
    public Texture2D testTexture;
#endif

    [DllImport("__Internal")]
    private static extern void UploadImage(string gameObjectName, string methodName);

    public void OpenFilePicker()
    {
        Debug.Log($"OpenFilePicker called on {gameObject.name}");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("Calling JavaScript UploadImage function...");
        UploadImage(gameObject.name, "OnImageReceived");
#else
        // if in the Editor, use the test texture instead.
        if (testTexture)
        {
            Debug.Log("Using test texture in Editor");
            OnImageReceived(testTexture);
        }
        else
        {
            Debug.Log("No test texture assigned in the Inspector. Cannot simulate upload.");
        }
#endif
    }

    // This overload is for the Editor test.
    public void OnImageReceived(Texture2D texture)
    {
        Debug.Log($"OnImageReceived called with texture: {texture.width}x{texture.height}");
        Debug.Log($"Number of listeners: {OnImageUploaded.GetPersistentEventCount()}");
        
        OnImageUploaded?.Invoke(texture);
    }

    // This method is called from the browser with the image data.
    public void OnImageReceived(string base64Image)
    {
        Debug.Log($"OnImageReceived called from JavaScript. Base64 length: {base64Image.Length}");
        
        try
        {
            var imageBytes = System.Convert.FromBase64String(base64Image);
            Debug.Log($"Decoded {imageBytes.Length} bytes from base64");
            
            var texture = new Texture2D(2, 2);
            var success = texture.LoadImage(imageBytes);
            
            if (success)
            {
                Debug.Log($"Successfully loaded image: {texture.width}x{texture.height}");
                texture.Apply();
                
                // invoke the event passing the new texture to any listeners.
                OnImageUploaded?.Invoke(texture);
            }
            else
            {
                Debug.LogError("Failed to load image from bytes");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing image: {e.Message}");
        }
    }
}