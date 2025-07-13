#nullable enable

using DG.Tweening;
using Interfaces;
using Managers;
using UnityEngine;

public class Plug : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject? pluggedIn;
    [SerializeField] private GameObject switchObject = null!;

    [SerializeField] private Vector3 switchOnRotation;
    [SerializeField] private Vector3 switchOffRotation;

    [SerializeField] private bool isOn;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource = null!;
    [SerializeField] private AudioClip audioClip = null!;

    private AudioManager _audioManager = null!;

    private void Start()
    {
        _audioManager = AudioManager.Instance;
    }

    public void OnInteract(GameObject interactor)
    {
        _audioManager.PlaySound(audioSource, audioClip);

        switchObject.transform.DOLocalRotate(isOn ? switchOffRotation : switchOnRotation, 1f);

        isOn = !isOn;

        if (!pluggedIn) return;

        if (pluggedIn.TryGetComponent(out ISwitchable s))
        {
            s.Toggle(isOn);
        }

    }

    public void SetPluggedIn(GameObject? plug)
    {
        pluggedIn = plug;
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return "Press E to toggle switch";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return "Tap to toggle switch";
    }

    public void ResetObject()
    {
        isOn = false;
        switchObject.transform.localRotation = Quaternion.Euler(switchOffRotation);

        if (pluggedIn && pluggedIn.TryGetComponent(out ISwitchable s))
        {
            s.Toggle(false);
        }
    }
}
