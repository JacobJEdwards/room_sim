using Managers;
using UnityEngine;

/// <summary>
/// Handles the player's camera rotation based on mouse or stick input.
/// Loads and saves sensitivity settings using PlayerPrefs.
/// </summary>
public class PlayerController : MonoBehaviour
{
    // Public key to access the sensitivity setting from other scripts (e.g., a settings manager).
    public const string MOUSE_SENS_KEY = "MouseSensitivity";

    [Header("Settings")]
    [Tooltip("The sensitivity of the camera movement. This value is loaded from PlayerPrefs.")]
    [SerializeField] private float _mouseSensitivity = 1f;

    [Header("Object References")]
    [Tooltip("The transform of the camera object to rotate vertically.")]
    [SerializeField] private Transform _cameraTransform;

    private Vector2 _lookInput;
    private float _xRotation = 0f;

    /// <summary>
    /// Locks the cursor and loads the saved sensitivity on start.
    /// </summary>
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Load the sensitivity from PlayerPrefs. If it's not set, use the default value of 1.
        _mouseSensitivity = PlayerPrefs.GetFloat(MOUSE_SENS_KEY, 1f);
    }

    /// <summary>
    /// Called every frame to update the camera's rotation.
    /// </summary>
    private void Update()
    {
        // Look(); // This has been disabled to prevent conflicts.
    }
    
    /// <summary>
    /// Sets the mouse sensitivity and saves it to PlayerPrefs.
    /// This can be called from a UI slider or settings manager.
    /// </summary>
    /// <param name="sensitivity">The new sensitivity value.</param>
    public void SetSensitivity(float sensitivity)
    {
        _mouseSensitivity = sensitivity;
        PlayerPrefs.SetFloat(MOUSE_SENS_KEY, sensitivity);
    }

    /// <summary>
    /// Processes look input and rotates the player and camera accordingly.
    /// This version uses raw input for a direct, responsive feel.
    /// </summary>
    private void Look()
    {
        _lookInput = InputManager.Instance.GetPitchYaw();

        float mouseX = _lookInput.x * _mouseSensitivity;
        float mouseY = _lookInput.y * _mouseSensitivity;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        _cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f); // Vertical rotation (Pitch)
        transform.Rotate(Vector3.up * mouseX); // Horizontal rotation (Yaw)
    }
}