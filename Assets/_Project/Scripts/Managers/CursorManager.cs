using UnityEngine;

namespace Project.Managers
{
    public sealed class CursorManager : MonoBehaviour
    {
        private UIManager uiManager;

        public void Initialize(UIManager newUIManager)
        {
            uiManager = newUIManager;
            EnableGameplayCursorMode();
        }

        public void EnableGameplayCursorMode()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (uiManager != null)
            {
                uiManager.SetGameplayHUDVisible(true);
            }
        }

        public void EnableMenuCursorMode()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (uiManager != null)
            {
                uiManager.SetGameplayHUDVisible(false);
            }
        }
    }
}