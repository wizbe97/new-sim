using TMPro;
using UnityEngine;

namespace Project.UI
{
    public sealed class BalanceDisplayView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI balanceText;

        [Header("Formatting")]
        [SerializeField] private string currencyPrefix = "$";

        public void SetBalance(int balance)
        {
            if (balanceText == null)
            {
                Debug.LogError($"{nameof(BalanceDisplayView)} on {name} is missing Balance Text.");
                return;
            }

            balanceText.text = $"Balance: {currencyPrefix}{balance:N0}";
        }
    }
}