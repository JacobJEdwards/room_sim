using DG.Tweening;
using Interfaces;
using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable
{
    [SerializeField] private Light[] lights;
    [SerializeField] private GameObject switchObject;
    [SerializeField] private Vector3 switchOnRotation;
    [SerializeField] private Vector3 switchOffRotation;

    private bool _on = true;

    public void OnInteract(GameObject interactor)
    {
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
        _on = true;
        switchObject.transform.DOLocalRotateQuaternion(Quaternion.Euler(switchOnRotation), 0.5f);
        // switchObject.transform.localRotation = Quaternion.Euler(switchOnRotation);
    }

    private void TurnOff()
    {
        foreach (var lght in lights)
        {
            lght.enabled = false;
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
}
