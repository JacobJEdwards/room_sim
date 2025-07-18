using DG.Tweening;
using Interfaces;
using Managers;
using UnityEngine;

public class DrawPull : MonoBehaviour, IInteractable
{
    [SerializeField] private float to = 0.5f;
    private bool _open;

    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private AudioSource audioSource;

    private Tween _tween;

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
        if (_tween != null && _tween.IsActive() && _tween.IsPlaying())
        {
            return;
        }

        _audioManager.PlaySound(audioSource, openClip);
        _tween = transform.DOLocalMoveX(to, 1f);
        _open = true;
    }

    private void Close()
    {
        if (_tween != null && _tween.IsActive() && _tween.IsPlaying())
        {
            return;
        }

        _audioManager.PlaySound(audioSource, closeClip);
        _tween = transform.DOLocalMoveX(0, 1f);
        _open = false;
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return _open ? "Press E to close drawer" : "Press E to open drawer";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return _open ? "Tap to close drawer" : "Tap to open drawer";
    }

    public void ResetObject()
    {
        Close();
    }
}
