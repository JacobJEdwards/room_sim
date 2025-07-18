using System;
using DG.Tweening;
using Interfaces;
using Managers;
using UnityEngine;

public class CupboardOpen : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject doorL;
    [SerializeField] private GameObject doorR;

    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioSource audioSource;

    private bool _open;

    private Tween _tweenLeft;
    private Tween _tweenRight;

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
        if (_tweenLeft != null && _tweenLeft.IsActive() && _tweenLeft.IsPlaying())
        {
            return;
        }

        _audioManager.PlaySound(audioSource, openSound);

        _tweenLeft = doorL.transform.DOLocalRotate(new Vector3(0, 90f, 0), 1f);
        _tweenRight = doorR.transform.DOLocalRotate(new Vector3(0, -90f, 0), 1f);
        _open = true;
    }

    private void Close()
    {
        if (_tweenLeft != null && _tweenLeft.IsActive() && _tweenLeft.IsPlaying())
        {
            return;
        }

        _audioManager.PlaySound(audioSource, closeSound);
        _tweenLeft = doorL.transform.DOLocalRotate(new Vector3(0, 0f, 0), 1f);
        _tweenRight = doorR.transform.DOLocalRotate(new Vector3(0, 0f, 0), 1f);
        _open = false;
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return _open ? "Press E to close cupboard" : "Press E to open cupboard";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return _open ? "Tap to Close Cupboard" : "Tap to Open Cupboard";
    }

    public void ResetObject()
    {
        Close();
    }
}
