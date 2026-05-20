using Project.CashDesk;
using UnityEngine;

namespace Project.Staff.Jobs
{
    public sealed class StaffCashDeskJobBehaviour : IStaffJobBehaviour
    {
        private readonly StaffJobContext context;

        private CashOutTicket activeTicket;
        private float activeTicketProcessStartTime = -1f;

        public StaffCashDeskJobBehaviour(StaffJobContext context)
        {
            this.context = context;
        }

        public void Tick()
        {
            if (context == null ||
                context.CashDeskManager == null ||
                context.Movement == null ||
                context.StaffMember == null)
            {
                Reset();
                return;
            }

            if (!context.CashDeskManager.TryGetStaffServicePosition(out Vector3 servicePosition, out Vector3 facingPosition))
            {
                Reset();
                return;
            }

            if (!context.Movement.HasReachedPosition(servicePosition))
            {
                Reset();
                context.Movement.SetDestination(servicePosition);
                return;
            }

            context.Movement.Stop();
            context.Movement.FacePosition(facingPosition);

            if (!context.CashDeskManager.TryGetFrontTicket(out CashOutTicket frontTicket))
            {
                Reset();
                return;
            }

            if (!frontTicket.IsPlacedOnDesk || frontTicket.IsPaid)
            {
                Reset();
                return;
            }

            if (activeTicket != frontTicket)
            {
                activeTicket = frontTicket;
                activeTicketProcessStartTime = Time.time;

                Debug.Log(
                    $"{context.StaffMember.StaffName} started processing ticket {frontTicket.name} at the cash desk. " +
                    $"Ticket placed at {frontTicket.PlacedOnDeskTime:0.00}, processing started at {activeTicketProcessStartTime:0.00}.",
                    context.Owner);
            }

            float earliestPayoutTime =
                activeTicketProcessStartTime + context.StaffMember.CashDeskSpeed;

            if (Time.time < earliestPayoutTime)
            {
                return;
            }

            bool paid = context.CashDeskManager.TryPayFrontTicket(context.StaffMember);

            if (paid)
            {
                Debug.Log($"{context.StaffMember.StaffName} paid the front cash desk ticket.", context.Owner);
                Reset();
            }
        }

        public void Reset()
        {
            activeTicket = null;
            activeTicketProcessStartTime = -1f;
        }
    }
}