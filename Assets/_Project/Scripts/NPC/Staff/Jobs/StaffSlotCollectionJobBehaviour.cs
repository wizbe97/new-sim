using System.Collections.Generic;
using Project.SlotMachines;
using UnityEngine;

namespace Project.Staff.Jobs
{
    public sealed class StaffSlotCollectionJobBehaviour : IStaffJobBehaviour
    {
        private enum SlotCollectionState
        {
            Searching,
            MovingToCollectionPoint,
            Collecting,
            WanderingBecauseNoWork
        }

        private readonly StaffJobContext context;
        private readonly StaffNoWorkWanderBehaviour noWorkWanderBehaviour;
        private readonly float collectionPointReachedDistance;

        private readonly List<SlotMachine> skippedSlotMachines = new();

        private SlotMachine targetSlotMachine;
        private Transform targetCollectionPoint;
        private Vector3 targetCollectionPosition;

        private SlotCollectionState state = SlotCollectionState.Searching;

        private float collectionCompleteTime;
        private float nextSearchTime;

        public StaffSlotCollectionJobBehaviour(
            StaffJobContext context,
            StaffNoWorkWanderBehaviour noWorkWanderBehaviour,
            float collectionPointReachedDistance)
        {
            this.context = context;
            this.noWorkWanderBehaviour = noWorkWanderBehaviour;
            this.collectionPointReachedDistance = Mathf.Max(0.05f, collectionPointReachedDistance);
        }

        public void Tick()
        {
            switch (state)
            {
                case SlotCollectionState.Searching:
                    UpdateSearching();
                    break;

                case SlotCollectionState.MovingToCollectionPoint:
                    UpdateMovingToCollectionPoint();
                    break;

                case SlotCollectionState.Collecting:
                    UpdateCollecting();
                    break;

                case SlotCollectionState.WanderingBecauseNoWork:
                    UpdateWanderingBecauseNoWork();
                    break;
            }
        }

        public void Reset()
        {
            ResetTargets();

            skippedSlotMachines.Clear();
            collectionCompleteTime = 0f;
            nextSearchTime = Time.time;
            state = SlotCollectionState.Searching;

            noWorkWanderBehaviour?.Reset();
        }

        private void UpdateSearching()
        {
            if (context == null ||
                context.SlotMachineManager == null ||
                context.StaffMember == null)
            {
                return;
            }

            if (Time.time < nextSearchTime)
            {
                return;
            }

            skippedSlotMachines.Clear();

            while (context.SlotMachineManager.TryGetCollectableMachine(skippedSlotMachines, out SlotMachine candidateMachine))
            {
                if (TryAssignReachableSlotMachine(candidateMachine))
                {
                    return;
                }

                skippedSlotMachines.Add(candidateMachine);
            }

            BeginNoWorkWandering();
        }

        private bool TryAssignReachableSlotMachine(SlotMachine candidateMachine)
        {
            if (candidateMachine == null)
            {
                return false;
            }

            if (!TryFindReachableCollectionPoint(
                    candidateMachine,
                    out Transform collectionPoint,
                    out Vector3 navMeshPosition))
            {
                Debug.LogWarning(
                    $"{context.StaffMember.StaffName} could not reach any staff collection point on {candidateMachine.name}. Trying another machine.",
                    context.Owner);

                return false;
            }

            targetSlotMachine = candidateMachine;
            targetCollectionPoint = collectionPoint;
            targetCollectionPosition = navMeshPosition;

            if (!context.Movement.SetDestination(targetCollectionPosition))
            {
                return false;
            }

            state = SlotCollectionState.MovingToCollectionPoint;

            Debug.Log(
                $"{context.StaffMember.StaffName} is going to {candidateMachine.name} via collection point {collectionPoint.name}. Stored Cash: £{candidateMachine.StoredCashFromDeposits}.",
                context.Owner);

            return true;
        }

        private bool TryFindReachableCollectionPoint(
            SlotMachine slotMachine,
            out Transform collectionPoint,
            out Vector3 navMeshPosition)
        {
            collectionPoint = null;
            navMeshPosition = Vector3.zero;

            if (slotMachine == null)
            {
                return false;
            }

            if (!slotMachine.HasAnyStaffCollectionPoint())
            {
                Debug.LogWarning(
                    $"{slotMachine.name} has no staff collection points assigned in the SlotMachine inspector.",
                    slotMachine);

                return false;
            }

            for (int i = 0; i < slotMachine.StaffCollectionPoints.Count; i++)
            {
                Transform candidatePoint = slotMachine.StaffCollectionPoints[i];

                if (candidatePoint == null)
                {
                    continue;
                }

                if (!context.Movement.TryGetReachableNavMeshPosition(candidatePoint.position, out Vector3 reachablePosition))
                {
                    continue;
                }

                collectionPoint = candidatePoint;
                navMeshPosition = reachablePosition;
                return true;
            }

            return false;
        }

        private void UpdateMovingToCollectionPoint()
        {
            if (!IsTargetSlotMachineStillValid())
            {
                Reset();
                return;
            }

            if (!context.Movement.HasReachedPosition(targetCollectionPosition, collectionPointReachedDistance) &&
                !context.Movement.HasAgentReachedDestination())
            {
                return;
            }

            BeginCollectionTimer();
        }

        private void BeginCollectionTimer()
        {
            context.Movement.Stop();
            context.Movement.FacePosition(targetSlotMachine.transform.position);

            float duration = Mathf.Max(0f, context.StaffMember.SlotCollectionSpeed);
            collectionCompleteTime = Time.time + duration;

            state = SlotCollectionState.Collecting;

            Debug.Log(
                $"{context.StaffMember.StaffName} started collecting cash from {targetSlotMachine.name}. Duration: {duration:0.00}s.",
                context.Owner);
        }

        private void UpdateCollecting()
        {
            if (!IsTargetSlotMachineStillValid())
            {
                Reset();
                return;
            }

            context.Movement.Stop();
            context.Movement.FacePosition(targetSlotMachine.transform.position);

            if (Time.time < collectionCompleteTime)
            {
                return;
            }

            int requestedAmount = context.StaffMember.GetSlotCollectionRequestAmount(
                targetSlotMachine.StoredCashFromDeposits);

            bool collected = context.SlotMachineManager.TryCollectSlotCash(
                targetSlotMachine,
                requestedAmount,
                out int collectedAmount,
                context.StaffMember);

            if (collected)
            {
                Debug.Log($"{context.StaffMember.StaffName} collected £{collectedAmount} from {targetSlotMachine.name}.", context.Owner);
            }
            else
            {
                Debug.LogWarning(
                    $"{context.StaffMember.StaffName} reached {targetSlotMachine.name}, but collection failed. " +
                    $"Requested: £{requestedAmount}, Stored Cash: £{targetSlotMachine.StoredCashFromDeposits}.",
                    context.Owner);
            }

            Reset();
        }

        private bool IsTargetSlotMachineStillValid()
        {
            if (targetSlotMachine == null)
            {
                return false;
            }

            if (targetSlotMachine.IsBeingMovedForPlacement)
            {
                return false;
            }

            return targetSlotMachine.StoredCashFromDeposits > 0;
        }

        private void BeginNoWorkWandering()
        {
            ResetTargets();

            nextSearchTime =
                Time.time + context.StaffMember.SlotCollectionSearchIntervalSeconds;

            state = SlotCollectionState.WanderingBecauseNoWork;

            noWorkWanderBehaviour?.Reset();

            Debug.Log($"{context.StaffMember.StaffName} found no reachable slot machines with cash to collect.", context.Owner);
        }

        private void UpdateWanderingBecauseNoWork()
        {
            if (context == null || context.StaffMember == null)
            {
                return;
            }

            if (Time.time >= nextSearchTime)
            {
                state = SlotCollectionState.Searching;
                return;
            }

            noWorkWanderBehaviour?.Tick();
        }

        private void ResetTargets()
        {
            targetSlotMachine = null;
            targetCollectionPoint = null;
            targetCollectionPosition = context != null && context.Owner != null
                ? context.Owner.transform.position
                : Vector3.zero;
        }
    }
}