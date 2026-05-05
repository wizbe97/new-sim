using System;
using UnityEngine;

namespace Project.Input
{
    public sealed class PlayerInputHandler : MonoBehaviour
    {
        private PlayerInputActions inputActions;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }

        public event Action JumpPressed;
        public event Action JumpReleased;

        public event Action SprintStarted;
        public event Action SprintEnded;

        public event Action InteractPressed;
        public event Action RotatePressed;
        public event Action CancelPressed;

        private void Awake()
        {
            inputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            inputActions.Player.Enable();

            inputActions.Player.Move.performed += OnMovePerformed;
            inputActions.Player.Move.canceled += OnMoveCanceled;

            inputActions.Player.Look.performed += OnLookPerformed;
            inputActions.Player.Look.canceled += OnLookCanceled;

            inputActions.Player.Jump.performed += OnJumpPerformed;
            inputActions.Player.Jump.canceled += OnJumpCanceled;

            inputActions.Player.Sprint.started += OnSprintStarted;
            inputActions.Player.Sprint.canceled += OnSprintCanceled;

            inputActions.Player.Interact.performed += OnInteractPerformed;
            inputActions.Player.Rotate.performed += OnRotatePerformed;
            inputActions.Player.Cancel.performed += OnCancelPerformed;
        }

        private void OnDisable()
        {
            inputActions.Player.Move.performed -= OnMovePerformed;
            inputActions.Player.Move.canceled -= OnMoveCanceled;

            inputActions.Player.Look.performed -= OnLookPerformed;
            inputActions.Player.Look.canceled -= OnLookCanceled;

            inputActions.Player.Jump.performed -= OnJumpPerformed;
            inputActions.Player.Jump.canceled -= OnJumpCanceled;

            inputActions.Player.Sprint.started -= OnSprintStarted;
            inputActions.Player.Sprint.canceled -= OnSprintCanceled;

            inputActions.Player.Interact.performed -= OnInteractPerformed;
            inputActions.Player.Rotate.performed -= OnRotatePerformed;
            inputActions.Player.Cancel.performed -= OnCancelPerformed;

            inputActions.Player.Disable();
        }

        private void OnDestroy()
        {
            inputActions?.Dispose();
        }

        private void OnMovePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            MoveInput = Vector2.zero;
        }

        private void OnLookPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            LookInput = context.ReadValue<Vector2>();
        }

        private void OnLookCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            LookInput = Vector2.zero;
        }

        private void OnJumpPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            JumpPressed?.Invoke();
        }

        private void OnJumpCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            JumpReleased?.Invoke();
        }

        private void OnSprintStarted(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            SprintStarted?.Invoke();
        }

        private void OnSprintCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            SprintEnded?.Invoke();
        }

        private void OnInteractPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            InteractPressed?.Invoke();
        }

        private void OnRotatePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            RotatePressed?.Invoke();
        }

        private void OnCancelPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            CancelPressed?.Invoke();
        }
    }
}