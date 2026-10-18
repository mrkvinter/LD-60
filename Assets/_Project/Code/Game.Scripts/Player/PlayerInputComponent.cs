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

        private Vector2 move;
        private Vector2 look;
        private bool jump;
        private bool sprint;
        private bool crouch;
        private bool isInteractHeld;
        private bool isTakeFromRackHeld;
        private bool isThrowHeld;

        public bool IsBlocked { get; set; }

        public Vector2 Move => IsBlocked ? Vector2.zero : move;
        public Vector2 Look => IsBlocked ? Vector2.zero : look;
        public bool IsPointerLook { get; private set; } = true;
        public bool Jump => !IsBlocked && jump;
        public bool Sprint => !IsBlocked && sprint;
        public bool Crouch => !IsBlocked && crouch;
        public bool IsInteractHeld => !IsBlocked && isInteractHeld;
        public bool IsTakeFromRackHeld => !IsBlocked && isTakeFromRackHeld;
        public bool IsThrowHeld => !IsBlocked && isThrowHeld;
        public float Scroll { get; private set; }

        public event Action InteractPressed;
        public event Action InteractReleased;
        public event Action TakeFromRackPressed;
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
            move = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            look = context.ReadValue<Vector2>();
            IsPointerLook = context.control?.device is Pointer;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            jump = context.ReadValueAsButton();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            sprint = context.ReadValueAsButton();
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.performed && !IsBlocked) crouch = !crouch;
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                isInteractHeld = true;
                if (!IsBlocked) InteractPressed?.Invoke();
            }
            else if (context.canceled)
            {
                isInteractHeld = false;
                InteractReleased?.Invoke();
            }
        }

        public void OnTakeFromRack(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                isTakeFromRackHeld = true;
                if (!IsBlocked) TakeFromRackPressed?.Invoke();
            }
            else if (context.canceled)
            {
                isTakeFromRackHeld = false;
            }
        }

        public void OnThrow(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                isThrowHeld = true;
                if (!IsBlocked) ThrowPressed?.Invoke();
            }
            else if (context.canceled)
            {
                isThrowHeld = false;
            }
        }

        public void OnScroll(InputAction.CallbackContext context)
        {
            Scroll = context.ReadValue<float>();

            if (context.performed && !IsBlocked && !Mathf.Approximately(Scroll, 0f))
            {
                Scrolled?.Invoke(Scroll > 0f ? 1 : -1);
            }
        }
    }
}
