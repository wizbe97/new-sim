using TMPro;
using UnityEngine;

namespace Project.UI
{
    public sealed class BalanceDisplayView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI balanceText;

        [Tooltip("Optional. Shows only when Reserve Fund is below its required minimum.")]
        [SerializeField] private TextMeshProUGUI reserveText;

        [Header("Formatting")]
        [SerializeField] private string currencyPrefix = "£";
        [SerializeField] private string casinoFundsFormat = "Casino Funds: {0}{1:N0}";
        [SerializeField] private string reserveWarningFormat = "Reserve Fund Low: {0}{1:N0} / {0}{2:N0}";

        private void Awake()
        {
            HideReserveWarning();
        }

        public void SetBalance(int balance)
        {
            SetFunds(balance, 0, 0);
        }

        public void SetFunds(int casinoFunds, int reserveFund, int minimumReserveFund)
        {
            if (balanceText == null)
            {
                Debug.LogError($"{nameof(BalanceDisplayView)} on {name} is missing Balance Text.", this);
                return;
            }

            casinoFunds = Mathf.Max(0, casinoFunds);
            reserveFund = Mathf.Max(0, reserveFund);
            minimumReserveFund = Mathf.Max(0, minimumReserveFund);

            balanceText.text = string.Format(
                casinoFundsFormat,
                currencyPrefix,
                casinoFunds);

            RefreshReserveWarning(reserveFund, minimumReserveFund);
        }

        private void RefreshReserveWarning(int reserveFund, int minimumReserveFund)
        {
            if (reserveText == null)
            {
                return;
            }

            bool showReserveWarning =
                minimumReserveFund > 0 &&
                reserveFund < minimumReserveFund;

            if (!showReserveWarning)
            {
                HideReserveWarning();
                return;
            }

            reserveText.gameObject.SetActive(true);
            reserveText.text = string.Format(
                reserveWarningFormat,
                currencyPrefix,
                reserveFund,
                minimumReserveFund);
        }

        private void HideReserveWarning()
        {
            if (reserveText == null)
            {
                return;
            }

            reserveText.text = string.Empty;
            reserveText.gameObject.SetActive(false);
        }
    }
}