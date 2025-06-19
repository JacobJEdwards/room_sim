#nullable enable
using UnityEngine;

namespace Interfaces
{
    public interface IInteractable
    {
        void OnInteract(GameObject interactor);
        bool CanInteract(GameObject interactor);
        string GetInteractionPromptMobile(GameObject interactor);
        string GetInteractionPromptDesktop(GameObject interactor);
    }
}