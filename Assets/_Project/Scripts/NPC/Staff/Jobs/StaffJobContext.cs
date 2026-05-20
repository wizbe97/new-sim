using Project.Managers;
using UnityEngine;

namespace Project.Staff.Jobs
{
    public sealed class StaffJobContext
    {
        public StaffJobContext(
            StaffJobController owner,
            StaffMember staffMember,
            StaffMovementController movement,
            SlotMachineManager slotMachineManager,
            CashDeskManager cashDeskManager,
            StaffJob currentJob,
            Transform idleStandPoint,
            Vector3 idleFallbackPosition,
            bool hasIdleFallbackPosition)
        {
            Owner = owner;
            StaffMember = staffMember;
            Movement = movement;
            SlotMachineManager = slotMachineManager;
            CashDeskManager = cashDeskManager;
            CurrentJob = currentJob;
            IdleStandPoint = idleStandPoint;
            IdleFallbackPosition = idleFallbackPosition;
            HasIdleFallbackPosition = hasIdleFallbackPosition;
        }

        public StaffJobController Owner { get; }
        public StaffMember StaffMember { get; }
        public StaffMovementController Movement { get; }
        public SlotMachineManager SlotMachineManager { get; }
        public CashDeskManager CashDeskManager { get; }
        public StaffJob CurrentJob { get; }

        public Transform IdleStandPoint { get; set; }
        public Vector3 IdleFallbackPosition { get; }
        public bool HasIdleFallbackPosition { get; }

        public Vector3 GetIdlePosition()
        {
            if (IdleStandPoint != null)
            {
                return IdleStandPoint.position;
            }

            if (HasIdleFallbackPosition)
            {
                return IdleFallbackPosition;
            }

            return Owner != null ? Owner.transform.position : Vector3.zero;
        }
    }
}