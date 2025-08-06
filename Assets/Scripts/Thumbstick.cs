// Scripts/Thumbstick.cs

using UnityEngine;
using UnityEngine.EventSystems;

public class Thumbstick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public static bool IsPointerOverThumbstick { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPointerOverThumbstick = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsPointerOverThumbstick = false;
    }
}