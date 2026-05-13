using TMPro;
using UnityEngine;

namespace Project.SlotMachines.UI
{
    public sealed class SlotMachineBalanceDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SlotMachine slotMachine;
        [SerializeField] private TMP_Text balanceText;

        [Header("Colours")]
        [SerializeField] private string totalProfitLossColour = "#00FF66";
        [SerializeField] private string currentSessionBalanceColour = "#FFA500";

        [Header("Display")]
        [SerializeField] private bool showPlusSignForProfit = false;

        private void Awake()
        {
            if (slotMachine == null)
            {
                slotMachine = GetComponentInParent<SlotMachine>();
            }

            if (balanceText == null)
            {
                balanceText = GetComponentInChildren<TMP_Text>(true);
            }
        }

        private void OnEnable()
        {
            if (slotMachine != null)
            {
                slotMachine.FinancialsChanged += HandleFinancialsChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (slotMachine != null)
            {
                slotMachine.FinancialsChanged -= HandleFinancialsChanged;
            }
        }

        private void HandleFinancialsChanged()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (balanceText == null)
            {
                return;
            }

            if (slotMachine == null)
            {
                balanceText.text =
                    $"<color={totalProfitLossColour}>£0</color> : <color={currentSessionBalanceColour}>£0</color>";
                return;
            }

            string totalProfitLossText = FormatSignedCurrency(
                slotMachine.DisplayedTotalProfitLoss,
                showPlusSignForProfit);

            string currentSessionBalanceText = FormatUnsignedCurrency(
                slotMachine.CurrentSessionCredit);

            balanceText.text =
                $"<color={totalProfitLossColour}>{totalProfitLossText}</color> : <color={currentSessionBalanceColour}>{currentSessionBalanceText}</color>";
        }

        private static string FormatUnsignedCurrency(int amount)
        {
            return $"£{Mathf.Max(0, amount)}";
        }

        private static string FormatSignedCurrency(int amount, bool showPlusSign)
        {
            if (amount > 0 && showPlusSign)
            {
                return $"+£{amount}";
            }

            if (amount >= 0)
            {
                return $"£{amount}";
            }

            return $"-£{Mathf.Abs(amount)}";
        }
    }
}