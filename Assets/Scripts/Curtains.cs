using DG.Tweening;
using Interfaces;
using Managers;
using UnityEngine;

public class Curtains : MonoBehaviour, IInteractable
{
    private bool _open = true;
    [SerializeField]
    private float maxScale = 9.5f;
    [SerializeField] private GameObject curtain;

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
        curtain.transform.DOScaleY(1f, 1f);
        _open = true;
    }

    private void Close()
    {
        curtain.transform.DOScaleY(maxScale, 1f);
        _open = false;
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return _open ? "Press E to Close Curtains" : "Press E to Open Curtains";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return _open ? "Tap to Close Curtains" : "Tap to Open Curtains";
    }

    public void ResetObject()
    {
        Open();
    }
}
