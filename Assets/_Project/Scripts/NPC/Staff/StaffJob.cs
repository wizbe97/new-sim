using System;
using UnityEngine;

namespace Project.Staff
{
    [Serializable]
    public sealed class StaffJob
    {
        [Header("Job")]
        [SerializeField] private StaffJobCategory category = StaffJobCategory.Idle;

        [Header("Slot Collection")]
        [Tooltip("How many seconds this staff member waits before checking again when no reachable slot machine needs collecting.")]
        [SerializeField, Min(0.1f)] private float slotCollectionSearchIntervalSeconds = 4f;

        [Tooltip("If enabled, the staff member collects all available cash from the slot machine.")]
        [SerializeField] private bool collectAllSlotCash = true;

        [Tooltip("Used only when Collect All Slot Cash is disabled.")]
        [SerializeField, Min(1)] private int slotCollectionAmount = 100;

        [Header("Cash Desk")]
        [Tooltip("How many seconds after the front ticket is placed on the desk before staff pay it.")]
        [SerializeField, Min(0.1f)] private float cashDeskPayoutIntervalSeconds = 2f;

        public StaffJobCategory Category => category;
        public float SlotCollectionSearchIntervalSeconds => slotCollectionSearchIntervalSeconds;
        public float CashDeskPayoutIntervalSeconds => cashDeskPayoutIntervalSeconds;
        public bool CollectAllSlotCash => collectAllSlotCash;
        public int SlotCollectionAmount => slotCollectionAmount;

        public void SetCategory(StaffJobCategory newCategory)
        {
            category = newCategory;
        }

        public int GetSlotCollectionRequestAmount(int availableAmount)
        {
            if (collectAllSlotCash)
            {
                return Mathf.Max(0, availableAmount);
            }

            return Mathf.Clamp(slotCollectionAmount, 0, Mathf.Max(0, availableAmount));
        }

#if UNITY_EDITOR
        public void Validate()
        {
            slotCollectionSearchIntervalSeconds = Mathf.Max(0.1f, slotCollectionSearchIntervalSeconds);
            cashDeskPayoutIntervalSeconds = Mathf.Max(0.1f, cashDeskPayoutIntervalSeconds);
            slotCollectionAmount = Mathf.Max(1, slotCollectionAmount);
        }
#endif
    }
}