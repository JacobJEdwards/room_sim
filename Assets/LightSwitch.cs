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
        switchObject.transform.localRotation = Quaternion.Euler(switchOnRotation);
    }

    private void TurnOff()
    {
        foreach (var lght in lights)
        {
            lght.enabled = false;
        }

        _on = false;
        switchObject.transform.localRotation = Quaternion.Euler(switchOffRotation);
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPrompt(GameObject interactor)
    {
        return "Press E to Turn On/Off Light";
    }
}
