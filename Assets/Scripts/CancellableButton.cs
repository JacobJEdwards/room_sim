// Scripts/CancellableButton.cs

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class CancellableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public UnityEvent onCancellableClick;

    private bool _isPointerDown;

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPointerDown = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerDown = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_isPointerDown)
        {
            onCancellableClick.Invoke();
        }
        _isPointerDown = false;
    }
}