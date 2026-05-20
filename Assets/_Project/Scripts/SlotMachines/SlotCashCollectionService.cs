using Project.Economy;
using Project.Progression;
using Project.SlotMachines;
using UnityEngine;

namespace Project.Managers
{
    public sealed class SlotCashCollectionService : MonoBehaviour
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
                Debug.LogError($"{nameof(SlotCashCollectionService)} cannot initialize because PlayerBalanceManager is missing.", this);
                return;
            }

            if (casinoProgressionManager == null)
            {
                Debug.LogError($"{nameof(SlotCashCollectionService)} cannot initialize because CasinoProgressionManager is missing.", this);
                return;
            }

            Debug.Log($"{nameof(SlotCashCollectionService)} initialized.", this);
        }

        public bool TryCollectSlotCash(
            SlotMachine slotMachine,
            int requestedAmount,
            out int collectedAmount,
            Object collector = null)
        {
            collectedAmount = 0;

            if (slotMachine == null)
            {
                Debug.LogWarning(
                    $"{nameof(SlotCashCollectionService)} cannot collect because SlotMachine is missing.",
                    collector != null ? collector : this);

                return false;
            }

            SlotMachineCashSource cashSource =
                slotMachine.GetComponent<SlotMachineCashSource>();

            if (cashSource == null)
            {
                cashSource = slotMachine.gameObject.AddComponent<SlotMachineCashSource>();
            }

            return TryCollectCash(
                cashSource,
                requestedAmount,
                out collectedAmount,
                collector);
        }

        public bool TryCollectCash(
            ICollectableCashSource cashSource,
            int requestedAmount,
            out int collectedAmount,
            Object collector = null)
        {
            collectedAmount = 0;

            if (cashSource == null)
            {
                Debug.LogWarning(
                    $"{nameof(SlotCashCollectionService)} cannot collect because cash source is missing.",
                    collector != null ? collector : this);

                return false;
            }

            if (playerBalanceManager == null)
            {
                Debug.LogError(
                    $"{nameof(SlotCashCollectionService)} cannot collect because PlayerBalanceManager is missing.",
                    this);

                return false;
            }

            if (requestedAmount <= 0)
            {
                return false;
            }

            if (!cashSource.CanCollectCash)
            {
                return false;
            }

            bool collected = cashSource.TryCollectCash(
                requestedAmount,
                out collectedAmount);

            if (!collected || collectedAmount <= 0)
            {
                return false;
            }

            playerBalanceManager.AddBalance(collectedAmount);

            AwardCashCollectedXp(cashSource, collectedAmount);

            Debug.Log(
                $"{GetCollectorName(collector)} collected £{collectedAmount} from {cashSource.CashSourceName}.",
                collector != null ? collector : this);

            return true;
        }

        private void AwardCashCollectedXp(
            ICollectableCashSource cashSource,
            int collectedAmount)
        {
            if (casinoProgressionManager == null ||
                cashSource == null ||
                collectedAmount <= 0)
            {
                return;
            }

            casinoProgressionManager.AddConfiguredXp(
                CasinoXpSource.SlotCashCollected,
                collectedAmount,
                cashSource.XpMultiplier);
        }

        private static string GetCollectorName(Object collector)
        {
            return collector != null ? collector.name : "Unknown collector";
        }
    }
}