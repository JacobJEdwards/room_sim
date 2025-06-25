#nullable enable

using UnityEngine;
using UnityEngine.Events;

namespace Managers
{
    public class InputManager : MonoBehaviour
    {
        public InputSystem PlayerControls = null!;
        public static InputManager Instance { get; private set; } = null!;

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            PlayerControls = new InputSystem();
            PlayerControls.Enable();
        }

        public void SetOnTabPressed(UnityAction action)
        {
            PlayerControls.Player.Previous.performed += _ => action.Invoke();
        }

        public void SetOnPKeyPressed(UnityAction action)
        {
             PlayerControls.Player.Cancel.performed += _ => action.Invoke();
        }

        public void SetOnRKeyPressed(UnityAction action)
        {
            PlayerControls.Player.Next.performed += _ => action.Invoke();
        }

        public float GetForward()
        {
            return PlayerControls.Player.Move.ReadValue<Vector2>().y;
        }

        public void SetOnLeftArrowPressed(UnityAction action)
        {
            PlayerControls.Player.ArrowLeft.performed += _ => action.Invoke();
        }

        public void SetOnRightArrowPressed(UnityAction action)
        {
            PlayerControls.Player.ArrowRight.performed += _ => action.Invoke();
        }

        public void SetOnUpArrowPressed(UnityAction action)
        {
            PlayerControls.Player.ArrowUp.performed += _ => action.Invoke();
        }

        public void SetOnDownArrowPressed(UnityAction action)
        {
            PlayerControls.Player.ArrowDown.performed += _ => action.Invoke();
        }

        public void SetOnLeftArrowReleased(UnityAction action)
        {
            PlayerControls.Player.ArrowLeft.canceled += _ => action.Invoke();
        }

        public void SetOnRightArrowReleased(UnityAction action)
        {
            PlayerControls.Player.ArrowRight.canceled += _ => action.Invoke();
        }

        public void SetOnUpArrowReleased(UnityAction action)
        {
            PlayerControls.Player.ArrowUp.canceled += _ => action.Invoke();
        }

        public void SetOnDownArrowReleased(UnityAction action)
        {
            PlayerControls.Player.ArrowDown.canceled += _ => action.Invoke();
        }

        public void SetOnCommaPressed(UnityAction action)
        {
            PlayerControls.Player.Comma.performed += _ => action.Invoke();
        }

        public void SetOnDotPressed(UnityAction action)
        {
            PlayerControls.Player.Dot.performed += _ => action.Invoke();
        }

        public void SetOnCommaReleased(UnityAction action)
        {
            PlayerControls.Player.Comma.canceled += _ => action.Invoke();
        }

        public void SetOnDotReleased(UnityAction action)
        {
            PlayerControls.Player.Dot.canceled += _ => action.Invoke();
        }


        public float GetStrafe()
        {
            return PlayerControls.Player.Move.ReadValue<Vector2>().x;
        }

        public Vector2 GetPitchYaw()
        {
            return PlayerControls.Player.Look.ReadValue<Vector2>();
        }

        public void SetOnInteractPressed(UnityAction action)
        {
            PlayerControls.Player.Interact.performed += _ => action.Invoke();
        }


        public void SetOnJumpPressed(UnityAction action)
        {
            PlayerControls.Player.Jump.performed += _ => action.Invoke();
        }

        public void SetOnSprintPress(UnityAction action)
        {
            PlayerControls.Player.Sprint.performed += _ => action.Invoke();
        }

        public void SetOnSprintRelease(UnityAction action)
        {
            PlayerControls.Player.Sprint.canceled += _ => action.Invoke();
        }
    }
}