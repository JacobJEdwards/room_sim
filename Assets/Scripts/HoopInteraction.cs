// Scripts/HoopInteraction.cs

using Interfaces;
using Managers;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MoveableObject))]
public class HoopInteraction : MonoBehaviour, IInteractable, IHasName
{
    public string Name => "Basketball Hoop";

    private MoveableObject _moveableObject;

    private void Awake()
    {
        _moveableObject = GetComponent<MoveableObject>();
    }

    // --- Public method for Mobile Controls ---
    public void ToggleMovement()
    {
        // This is called by the InteractionManager on mobile
        if (BasketballManager.Instance.IsInBasketballMode()) return;

        if (_moveableObject.IsHeld)
        {
            _moveableObject.Drop();
        }
        else
        {
            _moveableObject.Pickup();
        }
    }

    // --- IInteractable Implementation ---
    public void OnInteract(GameObject interactor)
    {
        // This is the primary action: playing basketball
        if (BasketballManager.Instance != null)
        {
            if (BasketballManager.Instance.IsInBasketballMode())
            {
                BasketballManager.Instance.ExitShootingMode();
            }
            else
            {
                BasketballManager.Instance.StartShootingMode();
            }
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        return !_moveableObject.IsHeld && (GameManager.Instance.CurrentMode == GameManager.ControlMode.Camera ||
                                           GameManager.Instance.CurrentMode == GameManager.ControlMode.Basketball);
    }
    

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        if (_moveableObject.IsHeld) return "Click to place hoop";

        return BasketballManager.Instance.IsInBasketballMode()
            ? "Press E to stop playing"
            : "Press E to play basketball";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        if (_moveableObject.IsHeld) return "Tap Pickup button to place";

        return BasketballManager.Instance.IsInBasketballMode()
            ? "Tap to stop playing"
            : "Tap to play basketball";
    }

    public void ResetObject()
    {
        if (BasketballManager.Instance && BasketballManager.Instance.IsInBasketballMode())
        {
            BasketballManager.Instance.ExitShootingMode();
        }

        if (_moveableObject.IsHeld)
        {
            _moveableObject.Drop();
        }

        _moveableObject.ResetObject();
    }
}