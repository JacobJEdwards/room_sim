using Interfaces;
using UnityEngine;

public class DoorButton : MonoBehaviour, IInteractable
{
    [SerializeField] private OpenDoor door;

    public void OnInteract(GameObject interactor)
    {
        if (!door.open)
        {
            door.Toggle();
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        return !door.open;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return door.open ? "Door is already open" : "Press E to Open Door";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return door.open ? "Door is already open" : "Tap to Open Door";
    }
}
