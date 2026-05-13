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
        [SerializeField] private Color storedCashColour = new Color(0f, 1f, 0.4f, 1f);
        [SerializeField] private Color currentSessionBalanceColour = new Color(1f, 0.55f, 0f, 1f);

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
                    $"<color=#{ToHtml(storedCashColour)}>£0</color> : <color=#{ToHtml(currentSessionBalanceColour)}>£0</color>";
                return;
            }

            balanceText.text =
                $"<color=#{ToHtml(storedCashColour)}>£{slotMachine.StoredCashFromDeposits}</color> : <color=#{ToHtml(currentSessionBalanceColour)}>£{slotMachine.CurrentSessionCredit}</color>";
        }

        private static string ToHtml(Color colour)
        {
            return ColorUtility.ToHtmlStringRGB(colour);
        }
    }
}