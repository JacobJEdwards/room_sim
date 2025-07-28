// Scripts/MusicObject.cs

using Interfaces;
using Managers;
using UnityEngine;

/// <summary>
/// This component allows a GameObject to function as an interactable music player.
/// It must be on an object that also has a MoveableObject component.
/// When interacted with, it will toggle the playback of an assigned audio clip
/// using the central AudioManager.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(MoveableObject))]
public class MusicObject : MonoBehaviour, IInteractable, IHasName
{
    [Header("Object Identity")]
    [SerializeField]
    [Tooltip("The name of the object, which will be displayed in UI prompts (e.g., 'Radio', 'Record Player').")]
    private string objectName = "Music Player";

    [Header("Audio Settings")]
    [SerializeField]
    [Tooltip("The audio clip that will be played when the object is interacted with.")]
    private AudioClip musicClip;

    // --- Private References ---
    private AudioSource _audioSource;
    private AudioManager _audioManager;
    private MoveableObject _moveableObject;

    // --- IHasName Implementation ---
    public string Name => objectName;

    private void Awake()
    {
        // This script requires these components to function, so we grab them here.
        // The [RequireComponent] attributes ensure they are always present in the editor.
        _audioSource = GetComponent<AudioSource>();
        _moveableObject = GetComponent<MoveableObject>();
    }

    private void Start()
    {
        // Get the singleton instance of the AudioManager to handle sound playback.
        _audioManager = AudioManager.Instance;
        if (!_audioManager)
        {
            Debug.LogError("MusicObject requires the AudioManager to be present in the scene.", this);
            enabled = false; // Disable script if AudioManager is missing.
        }
    }

    // --- IInteractable Implementation ---

    /// <summary>
    /// Called by the InteractionManager when the player interacts with this object.
    /// Toggles the playback of the assigned music clip.
    /// </summary>
    public void OnInteract(GameObject interactor)
    {
        if (!_audioManager || !musicClip || !_audioSource) return;

        if (_audioSource.isPlaying)
        {
            _audioManager.StopSound(_audioSource);
        }
        else
        {
            _audioManager.PlaySound(_audioSource, musicClip);
        }
    }

    /// <summary>
    /// Determines if the player is allowed to interact with this object.
    /// The player cannot interact if they are currently holding the object.
    /// </summary>
    public bool CanInteract(GameObject interactor)
    {
        // Prevent interaction if the object is being held.
        return !_moveableObject.IsHeld;
    }

    /// <summary>
    /// Provides the UI prompt text for desktop players.
    /// </summary>
    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        if (!_audioSource || _moveableObject.IsHeld) return ""; // Return empty if something is wrong or object is held.
        // Provide dynamic text based on whether the music is playing or not.
        return _audioSource.isPlaying ? $"Press E to stop {objectName}" : $"Press E to play {objectName}";
    }

    /// <summary>
    /// Provides the UI prompt text for mobile players.
    /// </summary>
    public string GetInteractionPromptMobile(GameObject interactor)
    {
        if (!_audioSource || _moveableObject.IsHeld) return ""; // Return empty if something is wrong or object is held.
        return _audioSource.isPlaying ? $"Tap to stop {objectName}" : $"Tap to play {objectName}";
    }

    /// <summary>
    /// Called when the room is reset. This stops the music from playing.
    /// </summary>
    public void ResetObject()
    {
        if (_audioManager && _audioSource && _audioSource.isPlaying)
        {
            _audioManager.StopSound(_audioSource);
        }
    }
}
