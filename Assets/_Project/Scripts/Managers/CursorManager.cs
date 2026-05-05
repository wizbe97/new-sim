using UnityEngine;

namespace Project.Managers
{
    public sealed class CursorManager : MonoBehaviour
    {
        private UIManager uiManager;

        public void Initialize(UIManager newUIManager)
        {
            if (newUIManager == null)
            {
                Debug.LogError($"{nameof(CursorManager)} cannot initialize because UIManager is missing.");
                return;
            }

            uiManager = newUIManager;
            EnableGameplayCursorMode();
        }

        public void EnableGameplayCursorMode()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (uiManager != null)
            {
                uiManager.SetReticleVisible(true);
            }
        }

        public void EnableMenuCursorMode()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (uiManager != null)
            {
                uiManager.SetReticleVisible(false);
            }
        }

        public void EnterGameplayMode()
        {
            EnableGameplayCursorMode();
        }

        public void EnterUIMode()
        {
            EnableMenuCursorMode();
        }
    }
}