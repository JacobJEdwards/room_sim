using DG.Tweening;
using Interfaces;
using Managers;
using UnityEngine;

public class OpenCloseToilet : MonoBehaviour, IInteractable
{
    private bool _open;

    [SerializeField]
    private GameObject seat;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    private AudioManager _audioManager;

    private void Start()
    {
        _audioManager = AudioManager.Instance;
    }

    public void OnInteract(GameObject interactor)
    {
        if (_open)
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
        seat.transform.DOLocalRotate(new Vector3(0, 0, 90f), 1f);
        _open = true;
    }

    private void Close()
    {
        _audioManager.PlaySound(audioSource, closeSound);
        seat.transform.DOLocalRotate(new Vector3(0, 0f, 0), 1f);
        _open = false;
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return _open ? "Press E to close toilet" : "Press E to open toilet";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return _open ? "Tap to close toilet" : "Tap to open toilet";
    }

    public void ResetObject()
    {
        Close();
    }
}
