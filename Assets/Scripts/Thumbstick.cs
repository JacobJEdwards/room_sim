// Scripts/Thumbstick.cs

using UnityEngine;
using UnityEngine.EventSystems;

// We now use IPointerDownHandler and IPointerUpHandler for more precise control.
public class Thumbstick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // This flag will be accessible from any script and will be true when the pointer is pressed down on this UI element.
    public static bool IsPointerOverThumbstick { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        // When the player's finger presses down on the thumbstick area, set the flag to true.
        IsPointerOverThumbstick = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // When the player's finger is lifted from the thumbstick area, set the flag to false.
        IsPointerOverThumbstick = false;
    }
}