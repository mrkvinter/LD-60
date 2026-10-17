using System;
using Code.Cursor;
using Code.Game.Scripts.EntitySystem;
using Game.Scripts.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Scripts.Player
{
    public sealed class PlayerInputComponent : IEntityComponent, PlayerInputActions.IPlayerActions, IDisposable
    {
        private readonly PlayerInputActions actions;
        private readonly CursorService cursorService;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool IsPointerLook { get; private set; } = true;
        public bool Jump { get; private set; }
        public bool Sprint { get; private set; }
        public bool Crouch { get; private set; }
        public bool IsInteractHeld { get; private set; }
        public bool IsThrowHeld { get; private set; }
        public float Scroll { get; private set; }

        public event Action InteractPressed;
        public event Action InteractReleased;
        public event Action ThrowPressed;
        public event Action<int> Scrolled;

        public PlayerInputComponent(CursorService cursorService)
        {
            this.cursorService = cursorService;

            actions = new PlayerInputActions();
            actions.Player.SetCallbacks(this);
            actions.Player.Enable();

            cursorService.RequestLock();
        }

        public void Dispose()
        {
            actions.Player.Disable();
            actions.Dispose();

            cursorService.ReleaseLock();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            Move = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            Look = context.ReadValue<Vector2>();
            IsPointerLook = context.control?.device is Pointer;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            Jump = context.ReadValueAsButton();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            Sprint = context.ReadValueAsButton();
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            Crouch = context.ReadValueAsButton();
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                IsInteractHeld = true;
                InteractPressed?.Invoke();
            }
            else if (context.canceled)
            {
                IsInteractHeld = false;
                InteractReleased?.Invoke();
            }
        }

        public void OnThrow(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                IsThrowHeld = true;
                ThrowPressed?.Invoke();
            }
            else if (context.canceled)
            {
                IsThrowHeld = false;
            }
        }

        public void OnScroll(InputAction.CallbackContext context)
        {
            Scroll = context.ReadValue<float>();

            if (context.performed && !Mathf.Approximately(Scroll, 0f))
            {
                Scrolled?.Invoke(Scroll > 0f ? 1 : -1);
            }
        }
    }
}
