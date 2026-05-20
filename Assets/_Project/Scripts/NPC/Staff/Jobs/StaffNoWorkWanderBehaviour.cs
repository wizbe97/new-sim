using UnityEngine;

namespace Project.Staff.Jobs
{
    public sealed class StaffNoWorkWanderBehaviour
    {
        private readonly StaffJobContext context;

        private float nextWanderTime;

        public StaffNoWorkWanderBehaviour(StaffJobContext context)
        {
            this.context = context;
        }

        public void Reset()
        {
            nextWanderTime = 0f;
        }

        public void Tick()
        {
            if (context == null ||
                context.Movement == null ||
                context.Owner == null ||
                context.StaffMember == null)
            {
                return;
            }

            if (!context.StaffMember.WanderWhenNoSlotCollectionWork)
            {
                return;
            }

            if (Time.time < nextWanderTime)
            {
                return;
            }

            if (context.Movement.TryGetRandomNavMeshPosition(
                    context.Owner.transform.position,
                    context.StaffMember.NoWorkWanderRadius,
                    out Vector3 wanderPosition))
            {
                context.Movement.SetDestination(wanderPosition);
            }

            nextWanderTime =
                Time.time + context.StaffMember.NoWorkWanderIntervalSeconds;
        }
    }
}