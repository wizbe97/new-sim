using Project.Economy;
using UnityEngine;

namespace Project.SlotMachines
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SlotMachine))]
    public sealed class SlotMachineCashSource : MonoBehaviour, ICollectableCashSource
    {
        [SerializeField] private SlotMachine slotMachine;

        public string CashSourceName =>
            slotMachine != null ? slotMachine.name : name;

        public int AvailableCash =>
            slotMachine != null ? slotMachine.StoredCashFromDeposits : 0;

        public bool CanCollectCash =>
            slotMachine != null &&
            !slotMachine.IsBeingMovedForPlacement &&
            slotMachine.StoredCashFromDeposits > 0;

        public float XpMultiplier
        {
            get
            {
                if (slotMachine == null || slotMachine.Config == null)
                {
                    return 1f;
                }

                return Mathf.Max(0f, slotMachine.Config.CasinoXpMultiplier);
            }
        }

        private void Awake()
        {
            CacheSlotMachine();
        }

        private void OnValidate()
        {
            CacheSlotMachine();
        }

        public bool TryCollectCash(int requestedAmount, out int collectedAmount)
        {
            collectedAmount = 0;

            if (!CanCollectCash)
            {
                return false;
            }

            if (requestedAmount <= 0)
            {
                return false;
            }

            return slotMachine.TryCollectStoredCash(
                requestedAmount,
                out collectedAmount);
        }

        private void CacheSlotMachine()
        {
            if (slotMachine != null)
            {
                return;
            }

            slotMachine = GetComponent<SlotMachine>();
        }
    }
}