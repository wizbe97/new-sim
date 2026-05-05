using Project.UI;
using UnityEngine;

namespace Project.Managers
{
    public sealed class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private HUDView hudPrefab;

        private HUDView hud;

        public HUDView HUD => hud;

        public void Initialize()
        {
            CreateHUD();
        }

        public void SetGameplayHUDVisible(bool isVisible)
        {
            if (hud == null)
            {
                return;
            }

            hud.SetReticleVisible(isVisible);
        }

        public void SetReticleInteractableState(bool hasInteractable)
        {
            if (hud == null)
            {
                return;
            }

            hud.SetReticleInteractableState(hasInteractable);
        }

        private void CreateHUD()
        {
            if (hudPrefab == null)
            {
                Debug.LogError($"{nameof(UIManager)} is missing HUD prefab.");
                return;
            }

            hud = Instantiate(hudPrefab);
            hud.name = "HUDCanvas";

            hud.ShowReticle();
            hud.SetReticleInteractableState(false);
        }
    }
}