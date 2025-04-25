#nullable enable

using DG.Tweening;
using Interfaces;
using UnityEngine;

public class Plug : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject? pluggedIn;
    [SerializeField] private GameObject switchObject = null!;

    [SerializeField] private Vector3 switchOnRotation;
    [SerializeField] private Vector3 switchOffRotation;

    [SerializeField] private bool isOn;

    public void OnInteract(GameObject interactor)
    {

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

    public string GetInteractionPrompt(GameObject interactor)
    {
        return "Press E to toggle switch";
    }
}
