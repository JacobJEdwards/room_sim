// Scripts/CancellableButton.cs

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// Add this script to any UI Image to make it behave like a button
// that cancels its action if the pointer is dragged off before release.
public class CancellableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    // You can hook into this event in the Inspector, just like a regular Button's onClick event.
    public UnityEvent onCancellableClick;

    private bool _isPointerDown;

    public void OnPointerDown(PointerEventData eventData)
    {
        // When the user first presses on the element, set a flag.
        _isPointerDown = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // If the user drags their pointer off the element, clear the flag.
        _isPointerDown = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // When the user releases the pointer, check if the flag is still true.
        // It will only be true if the pointer was released while still over the element.
        if (_isPointerDown)
        {
            // If the conditions are met, invoke the event.
            onCancellableClick.Invoke();
        }

        // Reset the flag.
        _isPointerDown = false;
    }
}