using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class HUDView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image reticleImage;

        [Header("Reticle Colors")]
        [SerializeField] private Color normalReticleColor = Color.white;
        [SerializeField] private Color interactableReticleColor = Color.green;

        public void ShowReticle()
        {
            SetReticleVisible(true);
        }

        public void HideReticle()
        {
            SetReticleVisible(false);
        }

        public void SetReticleVisible(bool isVisible)
        {
            if (reticleImage == null)
            {
                Debug.LogError($"{nameof(HUDView)} on {name} is missing Reticle Image.");
                return;
            }

            reticleImage.gameObject.SetActive(isVisible);
        }

        public void SetReticleInteractableState(bool hasInteractable)
        {
            if (reticleImage == null)
            {
                Debug.LogError($"{nameof(HUDView)} on {name} is missing Reticle Image.");
                return;
            }

            reticleImage.color = hasInteractable
                ? interactableReticleColor
                : normalReticleColor;
        }
    }
}