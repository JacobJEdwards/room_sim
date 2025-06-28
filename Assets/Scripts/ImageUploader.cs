// Scripts/ImageUploader.cs

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

public class ImageUploader : MonoBehaviour
{
    // This event will be invoked when an image is successfully uploaded.
    public UnityEvent<Texture2D> OnImageUploaded;

    [DllImport("__Internal")]
    private static extern void UploadImage(string gameObjectName, string methodName);

    public void OpenFilePicker()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UploadImage(gameObject.name, "OnImageReceived");
#else
        Debug.Log("File picker only works in WebGL builds.");
#endif
    }

    // This method is called from the browser with the image data.
    public void OnImageReceived(string base64Image)
    {
        var imageBytes = System.Convert.FromBase64String(base64Image);
        var texture = new Texture2D(2, 2);
        texture.LoadImage(imageBytes);
        texture.Apply();

        // Invoke the event, passing the new texture to any listeners.
        OnImageUploaded?.Invoke(texture);
    }
}