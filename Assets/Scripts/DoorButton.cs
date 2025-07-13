using Interfaces;
using Managers;
using UnityEngine;

public class DoorButton : MonoBehaviour, IInteractable
{
    [SerializeField] private OpenDoor door;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;

    private AudioManager _audioManager;

    private void Start()
    {
        _audioManager = AudioManager.Instance;
    }

    public void OnInteract(GameObject interactor)
    {
        _audioManager.PlaySound(audioSource, audioClip);

        if (!door.open)
        {
            door.Toggle();
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        return !door.open;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return door.open ? "Door is already open" : "Press E to Open Door";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return door.open ? "Door is already open" : "Tap to Open Door";
    }

    public void ResetObject()
    {
        door.ResetObject();
    }
}
