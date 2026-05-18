using Project.CashDesk;
using Project.NPC.Customer;
using Project.Progression;
using UnityEngine;

namespace Project.Managers
{
    public sealed class CashDeskPayoutService : MonoBehaviour
    {
        private PlayerBalanceManager playerBalanceManager;
        private CasinoProgressionManager casinoProgressionManager;

        public void Initialize(
            PlayerBalanceManager balanceManager,
            CasinoProgressionManager progressionManager)
        {
            playerBalanceManager = balanceManager;
            casinoProgressionManager = progressionManager;

            if (playerBalanceManager == null)
            {
                Debug.LogError($"{nameof(CashDeskPayoutService)} cannot initialize because PlayerBalanceManager is missing.", this);
                return;
            }

            if (casinoProgressionManager == null)
            {
                Debug.LogError($"{nameof(CashDeskPayoutService)} cannot initialize because CasinoProgressionManager is missing.", this);
                return;
            }

            Debug.Log($"{nameof(CashDeskPayoutService)} initialized.", this);
        }

        public bool TryPayCustomerTicket(
            CashOutTicket ticket,
            CustomerController customer,
            out int paidAmount,
            Object payer = null)
        {
            paidAmount = 0;

            if (ticket == null)
            {
                return false;
            }

            if (customer == null)
            {
                Debug.LogWarning($"{nameof(CashDeskPayoutService)} cannot pay ticket because customer is missing.", ticket);
                return false;
            }

            if (playerBalanceManager == null)
            {
                Debug.LogError($"{nameof(CashDeskPayoutService)} cannot pay ticket because PlayerBalanceManager is missing.", this);
                return false;
            }

            if (!playerBalanceManager.CanAffordCashOut(ticket.Amount))
            {
                Debug.LogWarning(
                    $"Cannot pay £{ticket.Amount} ticket. " +
                    $"Casino Funds: £{playerBalanceManager.CurrentCasinoFunds}, Reserve Fund: £{playerBalanceManager.CurrentReserveFund}.",
                    payer != null ? payer : this);

                return false;
            }

            paidAmount = ticket.Amount;

            bool paid = playerBalanceManager.TryPayCashOut(paidAmount);

            if (!paid)
            {
                paidAmount = 0;
                return false;
            }

            ticket.MarkPaid();
            customer.ReceiveCashOutPayment(paidAmount);
            AwardCustomerCashedOutXp(paidAmount);

            Debug.Log(
                $"{GetPayerName(payer)} paid {customer.name} a £{paidAmount} cash-out ticket.",
                payer != null ? payer : ticket);

            Object.Destroy(ticket.gameObject);

            return true;
        }

        private void AwardCustomerCashedOutXp(int paidAmount)
        {
            if (casinoProgressionManager == null || paidAmount <= 0)
            {
                return;
            }

            casinoProgressionManager.AddConfiguredXp(
                CasinoXpSource.CustomerCashedOut,
                paidAmount);
        }

        private static string GetPayerName(Object payer)
        {
            return payer != null ? payer.name : "Cash desk";
        }
    }
}