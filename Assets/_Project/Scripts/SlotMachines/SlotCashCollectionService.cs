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
                Debug.LogWarning($"{nameof(SlotCashCollectionService)} cannot collect because SlotMachine is missing.", collector);
                return false;
            }

            if (playerBalanceManager == null)
            {
                Debug.LogError($"{nameof(SlotCashCollectionService)} cannot collect because PlayerBalanceManager is missing.", this);
                return false;
            }

            if (requestedAmount <= 0)
            {
                return false;
            }

            bool collected = slotMachine.TryCollectStoredCash(
                requestedAmount,
                out collectedAmount);

            if (!collected || collectedAmount <= 0)
            {
                return false;
            }

            playerBalanceManager.AddBalance(collectedAmount);
            AwardSlotCashCollectedXp(slotMachine, collectedAmount);

            Debug.Log(
                $"{GetCollectorName(collector)} collected £{collectedAmount} from {slotMachine.name}.",
                collector != null ? collector : slotMachine);

            return true;
        }

        private void AwardSlotCashCollectedXp(SlotMachine slotMachine, int collectedAmount)
        {
            if (casinoProgressionManager == null || slotMachine == null || collectedAmount <= 0)
            {
                return;
            }

            float multiplier = GetSlotMachineXpMultiplier(slotMachine);

            casinoProgressionManager.AddConfiguredXp(
                CasinoXpSource.SlotCashCollected,
                collectedAmount,
                multiplier);
        }

        private static float GetSlotMachineXpMultiplier(SlotMachine slotMachine)
        {
            if (slotMachine == null || slotMachine.Config == null)
            {
                return 1f;
            }

            return Mathf.Max(0f, slotMachine.Config.CasinoXpMultiplier);
        }

        private static string GetCollectorName(Object collector)
        {
            return collector != null ? collector.name : "Unknown collector";
        }
    }
}