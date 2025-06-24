using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class ImageUploader : MonoBehaviour
{
    public RawImage displayImage;

    [DllImport("__Internal")]
    private static extern void UploadImage(string gameObjectName, string methodName);

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UploadImage(gameObject.name, nameof(OnImageUploaded));
#endif
    }

    public void OpenFilePicker()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UploadImage(gameObject.name, nameof(OnImageUploaded));
#else
        Debug.Log("File picker only works in WebGL builds.");
#endif
    }

    public void OnImageUploaded(string base64Image)
    {
        var imageBytes = System.Convert.FromBase64String(base64Image);
        var texture = new Texture2D(2, 2);
        texture.LoadImage(imageBytes);
        texture.Apply();

        displayImage.texture = texture;
    }
}