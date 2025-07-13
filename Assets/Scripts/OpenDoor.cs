using DG.Tweening;
using Interfaces;
using Managers;
using UnityEngine;

public class OpenDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioSource audioSource;

    private AudioManager _audioManager;

    private void Start()
    {
        _audioManager = AudioManager.Instance;
    }

    public bool open;

    public void OnInteract(GameObject interactor)
    {
        Toggle();
    }

    public void Toggle()
    {
        if (open)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    private void Open()
    {
        _audioManager.PlaySound(audioSource, openSound);
        transform.DOLocalRotate(new Vector3(0, 270f, 0), 1f);
        open = true;
    }

    private void Close()
    {
        _audioManager.PlaySound(audioSource, closeSound);
        transform.DOLocalRotate(new Vector3(0, 0f, 0), 1f);
        open = false;
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return open ? "Press E to close door" : "Press E to open door";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return open ? "Tap to close door" : "Tap to open door";
    }

    public void ResetObject()
    {
        Close();
    }
}
