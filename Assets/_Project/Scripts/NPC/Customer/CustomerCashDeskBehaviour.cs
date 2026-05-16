using System;
using System.Collections;
using Project.CashDesk;
using Project.Managers;
using UnityEngine;

namespace Project.NPC.Customer
{
    public sealed class CustomerCashDeskBehaviour : MonoBehaviour
    {
        private CustomerController owner;
        private CustomerRuntimeStats stats;
        private CustomerMovement movement;
        private CustomerTicketHolder ticketHolder;
        private CustomerDecisionLogic decisionLogic;

        private Action notifyMoneyChanged;
        private Action<int> cashOutPaymentReceived;
        private Action cashOutCompleted;
        private Action<CustomerState> setState;

        private CashDeskManager cashDeskManager;

        private Vector3 activeQueueTargetPosition;
        private Vector3 activeQueueFacingPosition;
        private bool hasActiveQueueTarget;
        private bool isFrontOfCashDeskQueue;
        private Transform activeTicketPlacementPoint;

        private bool isWaitingForCashDeskPayment;
        private bool hasPlacedTicketOnDesk;
        private bool hasCompletedCashDeskCashOut;

        public bool HasCompletedCashDeskCashOut => hasCompletedCashDeskCashOut;

        public void Initialize(
            CustomerController customerOwner,
            CustomerRuntimeStats runtimeStats,
            CustomerMovement customerMovement,
            CustomerTicketHolder customerTicketHolder,
            CustomerDecisionLogic customerDecisionLogic,
            Action onMoneyChanged,
            Action<int> onCashOutPaymentReceived,
            Action onCashOutCompleted,
            Action<CustomerState> setCustomerState)
        {
            owner = customerOwner;
            stats = runtimeStats;
            movement = customerMovement;
            ticketHolder = customerTicketHolder;
            decisionLogic = customerDecisionLogic;
            notifyMoneyChanged = onMoneyChanged;
            cashOutPaymentReceived = onCashOutPaymentReceived;
            cashOutCompleted = onCashOutCompleted;
            setState = setCustomerState;

            cashDeskManager = FindFirstObjectByType<CashDeskManager>();
        }

        private void Update()
        {
            UpdateCashDeskQueueMovement();
        }

        public IEnumerator CashOutHeldTicket()
        {
            if (ticketHolder == null || ticketHolder.HeldTicket == null)
            {
                yield break;
            }

            if (cashDeskManager == null)
            {
                cashDeskManager = FindFirstObjectByType<CashDeskManager>();
            }

            if (cashDeskManager == null)
            {
                Debug.LogWarning($"{name} has a ticket but no cash desk manager exists. Customer will keep waiting.", this);
                yield break;
            }

            setState?.Invoke(CustomerState.GoingToCashDesk);

            isWaitingForCashDeskPayment = true;
            hasPlacedTicketOnDesk = false;
            hasActiveQueueTarget = false;
            isFrontOfCashDeskQueue = false;

            cashDeskManager.QueueCustomer(owner, ticketHolder.HeldTicket);

            while (isWaitingForCashDeskPayment && ticketHolder.HeldTicket != null)
            {
                yield return null;
            }
        }

        public void SetCashDeskQueueTarget(
            Vector3 queueTargetPosition,
            Vector3 queueFacingPosition,
            bool isFrontOfQueue,
            Transform ticketPlacementPoint)
        {
            activeQueueTargetPosition = queueTargetPosition;
            activeQueueFacingPosition = queueFacingPosition;
            hasActiveQueueTarget = true;
            isFrontOfCashDeskQueue = isFrontOfQueue;
            activeTicketPlacementPoint = ticketPlacementPoint;

            movement?.SetDestination(activeQueueTargetPosition);

            setState?.Invoke(CustomerState.WaitingAtCashDesk);
        }

        public void ReceiveCashOutPayment(int amount)
        {
            stats.AddCashOutPaymentToWallet(amount);

            ticketHolder.ClearHeldTicketReference();

            isWaitingForCashDeskPayment = false;
            hasPlacedTicketOnDesk = false;
            hasActiveQueueTarget = false;
            isFrontOfCashDeskQueue = false;
            activeTicketPlacementPoint = null;

            hasCompletedCashDeskCashOut = true;

            decisionLogic.UpdateMoodFromCurrentMoney(0);
            notifyMoneyChanged?.Invoke();

            cashOutPaymentReceived?.Invoke(amount);
            cashOutCompleted?.Invoke();
        }

        private void UpdateCashDeskQueueMovement()
        {
            if (!isWaitingForCashDeskPayment || !hasActiveQueueTarget)
            {
                return;
            }

            if (movement == null || !movement.HasReachedDestination())
            {
                return;
            }

            movement.FacePosition(activeQueueFacingPosition);

            if (isFrontOfCashDeskQueue)
            {
                TryPlaceTicketOnDeskWhenReady();
            }
        }

        private void TryPlaceTicketOnDeskWhenReady()
        {
            if (hasPlacedTicketOnDesk ||
                ticketHolder == null ||
                ticketHolder.HeldTicket == null ||
                activeTicketPlacementPoint == null)
            {
                return;
            }

            ticketHolder.HeldTicket.PlaceOnDesk(activeTicketPlacementPoint);
            hasPlacedTicketOnDesk = true;

            Debug.Log($"{name} placed a £{ticketHolder.HeldTicket.Amount} ticket on the cash desk.", this);
        }
    }
}