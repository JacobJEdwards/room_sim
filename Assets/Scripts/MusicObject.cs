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
public class MusicObject : MonoBehaviour, IInteractable 
{
    [Header("Object Identity")]
    [SerializeField]
    [Tooltip("The name of the object, which will be displayed in UI prompts (e.g., 'Radio', 'Record Player').")]
    private string objectName = "Music Player";

    [Header("Audio Settings")]
    [SerializeField]
    [Tooltip("The audio clip that will be played when the object is interacted with.")]
    private AudioClip musicClip;

    private AudioSource _audioSource;
    private AudioManager _audioManager;
    private MoveableObject _moveableObject;


    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _moveableObject = GetComponent<MoveableObject>();
    }

    private void Start()
    {
        _audioManager = AudioManager.Instance;
        if (!_audioManager)
        {
            Debug.LogError("MusicObject requires the AudioManager to be present in the scene.", this);
            enabled = false; 
        }
    }


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
        if (!_audioSource || _moveableObject.IsHeld) return ""; 
        return _audioSource.isPlaying ? $"Press E to stop" : $"Press E to play";
    }

    /// <summary>
    /// Provides the UI prompt text for mobile players.
    /// </summary>
    public string GetInteractionPromptMobile(GameObject interactor)
    {
        if (!_audioSource || _moveableObject.IsHeld) return ""; 
        return _audioSource.isPlaying ? $"Tap to stop" : $"Tap to play";
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
