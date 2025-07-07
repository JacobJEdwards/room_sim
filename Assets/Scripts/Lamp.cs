using Interfaces;
using UnityEngine;

public class Lamp : MonoBehaviour, ISwitchable
{
    [SerializeField] private Light lightComp;

    public void Toggle(bool isOn)
    {
        lightComp.enabled = isOn;
    }
}
