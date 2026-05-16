using Project.CashDesk;
using UnityEngine;

namespace Project.NPC.Customer
{
    public sealed class CustomerTicketHolder : MonoBehaviour
    {
        [Header("Cash Out Ticket")]
        [SerializeField] private Vector3 ticketHandLocalPosition = new Vector3(0.35f, 0.6f, 0.25f);
        [SerializeField] private Vector3 ticketHandLocalEulerAngles = Vector3.zero;

        private CustomerController owner;
        private CashOutTicket heldTicket;
        private Transform ticketHandPoint;

        public CashOutTicket HeldTicket => heldTicket;
        public bool HasTicket => heldTicket != null;

        public int ActiveTicketValue
        {
            get
            {
                if (heldTicket == null || heldTicket.IsPaid)
                {
                    return 0;
                }

                return heldTicket.Amount;
            }
        }

        public void Initialize(CustomerController customerOwner)
        {
            owner = customerOwner;
            CreateTicketHandPoint();
        }

        public void CreateHeldTicket(int ticketAmount)
        {
            if (ticketAmount <= 0 || ticketHandPoint == null || owner == null)
            {
                return;
            }

            heldTicket = CashOutTicket.Create(ticketAmount, owner, ticketHandPoint);
            heldTicket.AttachToHand(ticketHandPoint);

            Debug.Log($"{name} printed and is holding a £{ticketAmount} cash-out ticket.", this);
        }

        public void ClearHeldTicketReference()
        {
            heldTicket = null;
        }

        private void CreateTicketHandPoint()
        {
            if (ticketHandPoint != null)
            {
                return;
            }

            GameObject handPointObject = new GameObject("TicketHandPoint");
            ticketHandPoint = handPointObject.transform;
            ticketHandPoint.SetParent(transform, false);
            ticketHandPoint.localPosition = ticketHandLocalPosition;
            ticketHandPoint.localRotation = Quaternion.Euler(ticketHandLocalEulerAngles);
        }
    }
}