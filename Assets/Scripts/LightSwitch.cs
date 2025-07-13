using System;
using DG.Tweening;
using Interfaces;
using Managers;
using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable
{
    [SerializeField] private Light[] lights;
    private Renderer[] _renderers;

    [SerializeField] private Material onMaterial;
    [SerializeField] private Material offMaterial;

    [SerializeField] private GameObject switchObject;
    [SerializeField] private Vector3 switchOnRotation;
    [SerializeField] private Vector3 switchOffRotation;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;

    private bool _on = true;

    private AudioManager _audioManager;

    private void Start()
    {
        for (var i = 0; i < lights.Length; i++)
        {
            var lght = lights[i];
            if (!lght) continue;
            lght.enabled = _on;
            var rndr = lght.GetComponent<Renderer>();
            if (!rndr) continue;
            _renderers ??= new Renderer[lights.Length];
            _renderers[i] = rndr;
            rndr.material = _on ? onMaterial : offMaterial;
        }

        _audioManager = AudioManager.Instance;
    }

    public void OnInteract(GameObject interactor)
    {
        _audioManager.PlaySound(audioSource, audioClip);

        if (_on)
        {
            TurnOff();
        }
        else
        {
            TurnOn();
        }
    }

    private void TurnOn()
    {
        foreach (var lght in lights)
        {
            lght.enabled = true;
        }
        foreach (var rndr in _renderers)
        {
            if (rndr)
            {
                rndr.material = onMaterial;
            }
        }
        _on = true;
        switchObject.transform.DOLocalRotateQuaternion(Quaternion.Euler(switchOnRotation), 0.5f);
    }

    private void TurnOff()
    {
        foreach (var lght in lights)
        {
            lght.enabled = false;
        }
        foreach (var rndr in _renderers)
        {
            if (rndr)
            {
                rndr.material = offMaterial;
            }
        }

        _on = false;
        switchObject.transform.DOLocalRotateQuaternion(Quaternion.Euler(switchOffRotation), 0.5f);
        // switchObject.transform.localRotation = Quaternion.Euler(switchOffRotation);
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return _on ? "Press E to Turn Light Off" : "Press E to Turn Light On";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return _on ? "Tap to Turn Light Off" : "Tap to Turn Light On";
    }

    public void ResetObject()
    {
        TurnOn();
    }
}
