using System;
using System.Collections;
using Project.SlotMachines;
using Project.World;
using Project.Managers;
using UnityEngine;
using UnityEngine.AI;

namespace Project.NPC.Customer
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CustomerMovement))]
    [RequireComponent(typeof(CustomerTicketHolder))]
    [RequireComponent(typeof(CustomerDecisionLogic))]
    [RequireComponent(typeof(CustomerSlotMachineBehaviour))]
    [RequireComponent(typeof(CustomerCashDeskBehaviour))]
    public sealed class CustomerController : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float searchRetryDelay = 1f;

        [Header("Runtime Debug")]
        [SerializeField] private CustomerState currentState = CustomerState.Uninitialized;
        [SerializeField] private CustomerRuntimeStats stats;

        private CustomerProfileSO profile;
        private CasinoEntrance entrance;
        private SlotMachineManager slotMachineManager;

        private CustomerMovement movement;
        private CustomerTicketHolder ticketHolder;
        private CustomerDecisionLogic decisionLogic;
        private CustomerSlotMachineBehaviour slotMachineBehaviour;
        private CustomerCashDeskBehaviour cashDeskBehaviour;

        private Vector3 exitPosition;
        private bool wantsToLeaveCasino;

        public event Action<CustomerController> CustomerMoneyChanged;
        public event Action<int> CashOutPaymentReceived;

        public CustomerRuntimeStats Stats => stats;
        public string CurrentStateName => currentState.ToString();
        public int ActiveTicketValue => ticketHolder != null ? ticketHolder.ActiveTicketValue : 0;

        public void Initialize(
            CustomerProfileSO customerProfile,
            CasinoEntrance casinoEntrance,
            SlotMachineManager machineManager,
            Vector3 leavePosition)
        {
            if (!CanInitialize(customerProfile, casinoEntrance, machineManager))
            {
                return;
            }

            profile = customerProfile;
            entrance = casinoEntrance;
            slotMachineManager = machineManager;
            exitPosition = leavePosition;

            stats = new CustomerRuntimeStats(
                profile.Stake,
                profile.GetRandomStartingBalance());

            CacheComponents();
            InitializeComponents();

            name = $"Customer_{profile.Stake}_£{stats.StartingBalance}";

            Debug.Log(
                $"{name} spawned. Stake: {stats.Stake}, Starting Balance: £{stats.StartingBalance}, Mood: {stats.Mood}.",
                this);

            NotifyMoneyChanged();

            StartCoroutine(CustomerRoutine());
        }

        private bool CanInitialize(
            CustomerProfileSO customerProfile,
            CasinoEntrance casinoEntrance,
            SlotMachineManager machineManager)
        {
            if (customerProfile == null)
            {
                Debug.LogError($"{nameof(CustomerController)} cannot initialize without a profile.", this);
                return false;
            }

            if (casinoEntrance == null)
            {
                Debug.LogError($"{nameof(CustomerController)} cannot initialize without a casino entrance.", this);
                return false;
            }

            if (machineManager == null)
            {
                Debug.LogError($"{nameof(CustomerController)} cannot initialize without a SlotMachineManager.", this);
                return false;
            }

            return true;
        }

        private void CacheComponents()
        {
            movement = GetComponent<CustomerMovement>();
            ticketHolder = GetComponent<CustomerTicketHolder>();
            decisionLogic = GetComponent<CustomerDecisionLogic>();
            slotMachineBehaviour = GetComponent<CustomerSlotMachineBehaviour>();
            cashDeskBehaviour = GetComponent<CustomerCashDeskBehaviour>();
        }

        private void InitializeComponents()
        {
            movement.Initialize(SetState);
            ticketHolder.Initialize(this);

            decisionLogic.Initialize(
                profile,
                stats,
                ticketHolder,
                slotMachineBehaviour.LogSessionSummary);

            slotMachineBehaviour.Initialize(
                this,
                profile,
                stats,
                slotMachineManager,
                ticketHolder,
                decisionLogic,
                NotifyMoneyChanged);

            cashDeskBehaviour.Initialize(
                this,
                stats,
                movement,
                ticketHolder,
                decisionLogic,
                NotifyMoneyChanged,
                InvokeCashOutPaymentReceived,
                HandleCashDeskCashOutCompleted,
                SetState);
        }

        private IEnumerator CustomerRoutine()
        {
            SetState(CustomerState.GoingToEntrance);

            yield return movement.MoveTo(entrance.Position);

            int noMachineWaitingActions = 0;

            while (!wantsToLeaveCasino && stats.WalletBalance > 0)
            {
                SetState(CustomerState.LookingForSlot);

                CustomerSlotSearchResult slotSearchResult =
                    slotMachineBehaviour.TryFindAndReserveMachine(out SlotMachine reservedSlotMachine);

                if (slotSearchResult == CustomerSlotSearchResult.NoMachineAvailable)
                {
                    noMachineWaitingActions++;

                    if (movement.LogIdleBehaviour)
                    {
                        Debug.Log(
                            $"{name} could not find an available slot machine. Waiting action {noMachineWaitingActions}/{profile.MaxNoMachineWaitingActionsBeforeLeaving}.",
                            this);
                    }

                    if (noMachineWaitingActions >= profile.MaxNoMachineWaitingActionsBeforeLeaving)
                    {
                        wantsToLeaveCasino = true;
                        break;
                    }

                    yield return movement.WaitInsideCasino(profile);
                    yield return new WaitForSeconds(searchRetryDelay);
                    continue;
                }

                if (slotSearchResult == CustomerSlotSearchResult.ReservationFailed)
                {
                    yield return movement.WaitInsideCasino(profile);
                    yield return new WaitForSeconds(searchRetryDelay);
                    continue;
                }

                noMachineWaitingActions = 0;

                SetState(CustomerState.GoingToSlot);

                yield return movement.MoveTo(reservedSlotMachine.PlayPoint.position);

                SetState(CustomerState.PlayingSlot);

                yield return slotMachineBehaviour.PlayReservedSlot();

                if (ticketHolder.HasTicket)
                {
                    yield return cashDeskBehaviour.CashOutHeldTicket();
                }

                if (cashDeskBehaviour.HasCompletedCashDeskCashOut)
                {
                    wantsToLeaveCasino = true;
                    break;
                }

                if (decisionLogic.ShouldLeaveCasinoAfterSession(cashDeskBehaviour.HasCompletedCashDeskCashOut))
                {
                    wantsToLeaveCasino = true;
                    break;
                }

                slotMachineBehaviour.ClearReservedSlotReference();

                if (UnityEngine.Random.value < profile.WanderAfterMachineChance)
                {
                    yield return movement.WaitInsideCasino(profile);
                }
            }

            decisionLogic.UpdateMoodFromCurrentMoney(0);

            yield return LeaveCasino();
        }

        private IEnumerator LeaveCasino()
        {
            SetState(CustomerState.Leaving);

            slotMachineBehaviour.ReleaseReservation();

            Debug.Log(
                $"{name} is leaving casino. Starting Balance: £{stats.StartingBalance}, Final Wallet: £{stats.WalletBalance}, Net Result: £{stats.WalletBalance - stats.StartingBalance}, Mood: {stats.Mood}, Lifetime Spins: {stats.SpinsPlayed}.",
                this);

            yield return movement.MoveTo(exitPosition);

            SetState(CustomerState.Finished);

            Destroy(gameObject);
        }

        public void SetCashDeskQueueTarget(
            Vector3 queueTargetPosition,
            Vector3 queueFacingPosition,
            bool isFrontOfQueue,
            Transform ticketPlacementPoint)
        {
            cashDeskBehaviour.SetCashDeskQueueTarget(
                queueTargetPosition,
                queueFacingPosition,
                isFrontOfQueue,
                ticketPlacementPoint);
        }

        public void ReceiveCashOutPayment(int amount)
        {
            cashDeskBehaviour.ReceiveCashOutPayment(amount);
        }

        public void NotifySlotMachineBecameUnavailable(SlotMachine slotMachine, int ticketAmount)
        {
            slotMachineBehaviour.NotifySlotMachineBecameUnavailable(slotMachine, ticketAmount);
        }

        private void HandleCashDeskCashOutCompleted()
        {
            wantsToLeaveCasino = true;

            SetState(CustomerState.Leaving);

            movement.BeginImmediateMove(exitPosition);
        }

        private void NotifyMoneyChanged()
        {
            CustomerMoneyChanged?.Invoke(this);
        }

        private void InvokeCashOutPaymentReceived(int amount)
        {
            CashOutPaymentReceived?.Invoke(amount);
        }

        private void SetState(CustomerState newState)
        {
            currentState = newState;
        }

        private void OnDestroy()
        {
            if (slotMachineBehaviour != null)
            {
                slotMachineBehaviour.ReleaseReservation();
            }
        }
    }
}