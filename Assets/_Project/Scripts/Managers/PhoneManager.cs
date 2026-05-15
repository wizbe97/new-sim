using Project.Input;
using Project.Player;
using UnityEngine;

namespace Project.Managers
{
    public sealed class PhoneManager : MonoBehaviour
    {
        private PlayerInputHandler playerInputHandler;
        private FirstPersonController firstPersonController;
        private PlayerInteractionController playerInteractionController;

        private UIManager uiManager;
        private CursorManager cursorManager;

        private bool isPhoneOpen;

        public bool IsPhoneOpen => isPhoneOpen;

        public void Initialize(GameObject player, UIManager uiManager, CursorManager cursorManager)
        {
            if (player == null)
            {
                Debug.LogError($"{nameof(PhoneManager)} cannot initialize because player is missing.");
                return;
            }

            Initialize(
                player.GetComponentInChildren<PlayerInputHandler>(),
                player.GetComponentInChildren<FirstPersonController>(),
                player.GetComponentInChildren<PlayerInteractionController>(),
                uiManager,
                cursorManager
            );
        }

        public void Initialize(Component player, UIManager uiManager, CursorManager cursorManager)
        {
            if (player == null)
            {
                Debug.LogError($"{nameof(PhoneManager)} cannot initialize because player is missing.");
                return;
            }

            Initialize(
                player.GetComponentInChildren<PlayerInputHandler>(),
                player.GetComponentInChildren<FirstPersonController>(),
                player.GetComponentInChildren<PlayerInteractionController>(),
                uiManager,
                cursorManager
            );
        }

        private void Initialize(
            PlayerInputHandler inputHandler,
            FirstPersonController movementController,
            PlayerInteractionController interactionController,
            UIManager newUIManager,
            CursorManager newCursorManager)
        {
            if (inputHandler == null)
            {
                Debug.LogError($"{nameof(PhoneManager)} cannot initialize because PlayerInputHandler is missing on the player.");
                return;
            }

            if (movementController == null)
            {
                Debug.LogError($"{nameof(PhoneManager)} cannot initialize because FirstPersonController is missing on the player.");
                return;
            }

            if (interactionController == null)
            {
                Debug.LogError($"{nameof(PhoneManager)} cannot initialize because PlayerInteractionController is missing on the player.");
                return;
            }

            if (newUIManager == null)
            {
                Debug.LogError($"{nameof(PhoneManager)} cannot initialize because UIManager is missing.");
                return;
            }

            if (newCursorManager == null)
            {
                Debug.LogError($"{nameof(PhoneManager)} cannot initialize because CursorManager is missing.");
                return;
            }

            playerInputHandler = inputHandler;
            firstPersonController = movementController;
            playerInteractionController = interactionController;

            uiManager = newUIManager;
            cursorManager = newCursorManager;

            playerInputHandler.PhoneMenuPressed += HandlePhoneMenuPressed;
            playerInputHandler.CancelPressed += HandleCancelPressed;

            uiManager.PhoneClosePressed += HandlePhoneClosePressed;

            ClosePhone();
        }

        private void HandlePhoneMenuPressed()
        {
            TogglePhone();
        }

        private void HandleCancelPressed()
        {
            if (!isPhoneOpen)
            {
                return;
            }

            ClosePhone();
        }

        private void HandlePhoneClosePressed()
        {
            if (!isPhoneOpen)
            {
                return;
            }

            ClosePhone();
        }

        private void TogglePhone()
        {
            if (isPhoneOpen)
            {
                ClosePhone();
            }
            else
            {
                OpenPhone();
            }
        }

        private void OpenPhone()
        {
            isPhoneOpen = true;

            firstPersonController.SetCanMove(false);
            playerInteractionController.SetCanInteract(false);

            uiManager.ShowPhone();
            uiManager.ShowPhoneHomePage();

            cursorManager.EnableMenuCursorMode();
        }

        private void ClosePhone()
        {
            isPhoneOpen = false;

            if (firstPersonController != null)
            {
                firstPersonController.SetCanMove(true);
            }

            if (playerInteractionController != null)
            {
                playerInteractionController.SetCanInteract(true);
            }

            if (uiManager != null)
            {
                uiManager.HidePhone();
            }

            if (cursorManager != null)
            {
                cursorManager.EnableGameplayCursorMode();
            }
        }

        private void OnDestroy()
        {
            if (playerInputHandler != null)
            {
                playerInputHandler.PhoneMenuPressed -= HandlePhoneMenuPressed;
                playerInputHandler.CancelPressed -= HandleCancelPressed;
            }

            if (uiManager != null)
            {
                uiManager.PhoneClosePressed -= HandlePhoneClosePressed;
            }
        }
    }
}