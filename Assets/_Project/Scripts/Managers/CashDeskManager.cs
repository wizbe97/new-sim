using System.Collections;
using System.Collections.Generic;
using Project.CashDesk;
using Project.NPC.Customer;
using Project.Progression;
using UnityEngine;
using UnityEngine.AI;

namespace Project.Managers
{
    public sealed class CashDeskManager : MonoBehaviour
    {
        private sealed class QueueEntry
        {
            public CustomerController Customer;
            public CashOutTicket Ticket;

            public QueueEntry(CustomerController customer, CashOutTicket ticket)
            {
                Customer = customer;
                Ticket = ticket;
            }
        }

        [Header("Queue Generation")]
        [Tooltip("First queue position. Its forward direction is used as the direction the queue extends backwards.")]
        [SerializeField] private Transform queueStartPoint;

        [Tooltip("Customers will face this point when they reach their queue position. Usually the desk or ticket placement point.")]
        [SerializeField] private Transform queueFacingTarget;

        [Tooltip("Distance between customers in the queue.")]
        [SerializeField, Min(0.25f)] private float queueSpacing = 1.25f;

        [Tooltip("How far from the generated point the manager can search for a nearby NavMesh position.")]
        [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 1.5f;

        [Tooltip("Maximum number of queue positions the manager will generate.")]
        [SerializeField, Min(1)] private int maximumQueueSize = 30;

        [Tooltip("Delay before the remaining queue moves forward after the front customer has been paid.")]
        [SerializeField, Min(0f)] private float queueAdvanceDelaySeconds = 1f;

        [Header("Ticket Placement")]
        [SerializeField] private Transform ticketPlacementPoint;

        [Header("Economy")]
        [SerializeField] private PlayerBalanceManager playerBalanceManager;

        private CasinoProgressionManager casinoProgressionManager;

        private readonly List<QueueEntry> queue = new();
        private Coroutine delayedQueueUpdateRoutine;

        public int QueueCount => queue.Count;
        public bool HasSceneBindings => queueStartPoint != null && ticketPlacementPoint != null;

        public void Initialize(
            PlayerBalanceManager balanceManager,
            Transform newQueueStartPoint,
            Transform newTicketPlacementPoint,
            CasinoProgressionManager newCasinoProgressionManager)
        {
            playerBalanceManager = balanceManager;
            queueStartPoint = newQueueStartPoint;
            ticketPlacementPoint = newTicketPlacementPoint;
            casinoProgressionManager = newCasinoProgressionManager;

            if (queueFacingTarget == null)
            {
                queueFacingTarget = ticketPlacementPoint;
            }

            if (playerBalanceManager == null)
            {
                Debug.LogError($"{nameof(CashDeskManager)} cannot initialize because PlayerBalanceManager is missing.", this);
                return;
            }

            if (queueStartPoint == null)
            {
                Debug.LogError($"{nameof(CashDeskManager)} cannot initialize because Queue Start Point is missing.", this);
                return;
            }

            if (ticketPlacementPoint == null)
            {
                Debug.LogError($"{nameof(CashDeskManager)} cannot initialize because Ticket Placement Point is missing.", this);
                return;
            }

            if (casinoProgressionManager == null)
            {
                Debug.LogError($"{nameof(CashDeskManager)} cannot initialize because CasinoProgressionManager is missing.", this);
                return;
            }

            Debug.Log($"{nameof(CashDeskManager)} initialized.", this);
        }

        public void ClearSceneBindings()
        {
            if (delayedQueueUpdateRoutine != null)
            {
                StopCoroutine(delayedQueueUpdateRoutine);
                delayedQueueUpdateRoutine = null;
            }

            queue.Clear();

            queueStartPoint = null;
            queueFacingTarget = null;
            ticketPlacementPoint = null;
        }

        public void QueueCustomer(CustomerController customer, CashOutTicket ticket)
        {
            if (customer == null || ticket == null)
            {
                return;
            }

            if (!HasSceneBindings)
            {
                Debug.LogWarning($"{nameof(CashDeskManager)} cannot queue customer because this scene has no cash desk bindings.", this);
                return;
            }

            if (queue.Count >= maximumQueueSize)
            {
                Debug.LogWarning($"{nameof(CashDeskManager)} queue is full. {customer.name} cannot join.", this);
                return;
            }

            ticket.AssignCashDesk(this);

            if (IsCustomerAlreadyQueued(customer))
            {
                return;
            }

            queue.Add(new QueueEntry(customer, ticket));

            if (delayedQueueUpdateRoutine == null)
            {
                UpdateQueueLayout();
            }

            Debug.Log($"{customer.name} joined the cash desk queue with a £{ticket.Amount} ticket.", this);
        }

        public bool IsFrontTicket(CashOutTicket ticket)
        {
            RemoveInvalidQueueEntries(updateLayoutIfChanged: false);

            return queue.Count > 0 && queue[0].Ticket == ticket;
        }

        public bool TryPayTicket(CashOutTicket ticket)
        {
            RemoveInvalidQueueEntries(updateLayoutIfChanged: false);

            if (ticket == null || !IsFrontTicket(ticket))
            {
                return false;
            }

            QueueEntry paidEntry = queue[0];

            if (paidEntry.Customer == null)
            {
                RemoveFrontEntryAndDelayRemainingQueue();
                return false;
            }

            if (playerBalanceManager == null)
            {
                Debug.LogError($"{nameof(CashDeskManager)} cannot pay ticket because PlayerBalanceManager is missing.", this);
                return false;
            }

            if (!playerBalanceManager.CanAffordCashOut(ticket.Amount))
            {
                Debug.LogWarning(
                    $"Cannot pay £{ticket.Amount} ticket. " +
                    $"Casino Funds: £{playerBalanceManager.CurrentCasinoFunds}, Reserve Fund: £{playerBalanceManager.CurrentReserveFund}.",
                    this);

                return false;
            }

            int paidAmount = ticket.Amount;

            bool paid = playerBalanceManager.TryPayCashOut(paidAmount);

            if (!paid)
            {
                return false;
            }

            ticket.MarkPaid();

            queue.RemoveAt(0);

            paidEntry.Customer.ReceiveCashOutPayment(paidAmount);

            AwardCustomerCashedOutXp(paidAmount);

            Debug.Log($"{paidEntry.Customer.name} was paid £{paidAmount} at the cash desk.", this);

            Destroy(ticket.gameObject);

            DelayRemainingQueueAdvance();

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

        private bool IsCustomerAlreadyQueued(CustomerController customer)
        {
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].Customer == customer)
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveFrontEntryAndDelayRemainingQueue()
        {
            if (queue.Count > 0)
            {
                queue.RemoveAt(0);
            }

            DelayRemainingQueueAdvance();
        }

        private void DelayRemainingQueueAdvance()
        {
            if (delayedQueueUpdateRoutine != null)
            {
                StopCoroutine(delayedQueueUpdateRoutine);
                delayedQueueUpdateRoutine = null;
            }

            if (queue.Count <= 0 || !HasSceneBindings)
            {
                return;
            }

            delayedQueueUpdateRoutine = StartCoroutine(DelayedQueueUpdateRoutine());
        }

        private IEnumerator DelayedQueueUpdateRoutine()
        {
            yield return new WaitForSeconds(queueAdvanceDelaySeconds);

            delayedQueueUpdateRoutine = null;
            UpdateQueueLayout();
        }

        private void RemoveInvalidQueueEntries(bool updateLayoutIfChanged)
        {
            bool removedAny = false;

            for (int i = queue.Count - 1; i >= 0; i--)
            {
                if (queue[i].Customer == null || queue[i].Ticket == null)
                {
                    queue.RemoveAt(i);
                    removedAny = true;
                }
            }

            if (removedAny && updateLayoutIfChanged && delayedQueueUpdateRoutine == null)
            {
                UpdateQueueLayout();
            }
        }

        private void UpdateQueueLayout()
        {
            if (!HasSceneBindings)
            {
                return;
            }

            RemoveInvalidQueueEntries(updateLayoutIfChanged: false);

            for (int i = 0; i < queue.Count; i++)
            {
                QueueEntry entry = queue[i];

                Vector3 queuePosition = GetQueuePosition(i);
                Vector3 facingPosition = GetQueueFacingPosition();
                bool isFront = i == 0;

                entry.Customer.SetCashDeskQueueTarget(
                    queuePosition,
                    facingPosition,
                    isFront,
                    ticketPlacementPoint);
            }
        }

        private Vector3 GetQueuePosition(int queueIndex)
        {
            if (queueStartPoint == null)
            {
                return transform.position;
            }

            Vector3 rawPosition =
                queueStartPoint.position +
                queueStartPoint.forward.normalized * queueSpacing * queueIndex;

            if (NavMesh.SamplePosition(rawPosition, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return rawPosition;
        }

        private Vector3 GetQueueFacingPosition()
        {
            if (queueFacingTarget != null)
            {
                return queueFacingTarget.position;
            }

            if (ticketPlacementPoint != null)
            {
                return ticketPlacementPoint.position;
            }

            if (queueStartPoint != null)
            {
                return queueStartPoint.position - queueStartPoint.forward;
            }

            return transform.position;
        }
    }
}