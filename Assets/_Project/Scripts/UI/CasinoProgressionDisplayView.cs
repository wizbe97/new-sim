using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class CasinoProgressionDisplayView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI xpText;
        [SerializeField] private Slider xpSlider;

        [Header("Formatting")]
        [SerializeField] private string levelFormat = "Casino Level {0}";
        [SerializeField] private string xpFormat = "{0:N0} / {1:N0} XP";

        public void SetProgression(int level, int currentLevelXp, int xpRequiredForNextLevel)
        {
            level = Mathf.Max(1, level);
            currentLevelXp = Mathf.Max(0, currentLevelXp);
            xpRequiredForNextLevel = Mathf.Max(0, xpRequiredForNextLevel);

            if (levelText != null)
            {
                levelText.text = string.Format(levelFormat, level);
            }

            if (xpText != null)
            {
                xpText.text = xpRequiredForNextLevel > 0
                    ? string.Format(xpFormat, currentLevelXp, xpRequiredForNextLevel)
                    : "Max Level";
            }

            if (xpSlider != null)
            {
                xpSlider.minValue = 0f;
                xpSlider.maxValue = xpRequiredForNextLevel > 0 ? xpRequiredForNextLevel : 1f;
                xpSlider.value = xpRequiredForNextLevel > 0 ? Mathf.Clamp(currentLevelXp, 0, xpRequiredForNextLevel) : 1f;
                xpSlider.interactable = false;
            }
        }
    }
}