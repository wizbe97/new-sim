using System;
using System.Collections;
using Project.SlotMachines;
using Project.Managers;
using UnityEngine;

namespace Project.NPC.Customer
{
    public sealed class CustomerSlotMachineBehaviour : MonoBehaviour
    {
        [Header("Debug Logging")]
        [SerializeField] private bool logEverySpin = true;
        [SerializeField] private bool logSessionSummary = true;

        private CustomerController owner;
        private CustomerProfileSO profile;
        private CustomerRuntimeStats stats;
        private SlotMachineManager slotMachineManager;
        private CustomerTicketHolder ticketHolder;
        private CustomerDecisionLogic decisionLogic;
        private Action notifyMoneyChanged;

        private SlotMachine reservedSlotMachine;
        private bool currentSlotMachineBecameUnavailable;
        private int pendingMachineUnavailableTicketAmount;

        public bool LogSessionSummary => logSessionSummary;
        public SlotMachine ReservedSlotMachine => reservedSlotMachine;

        public void Initialize(
            CustomerController customerOwner,
            CustomerProfileSO customerProfile,
            CustomerRuntimeStats runtimeStats,
            SlotMachineManager machineManager,
            CustomerTicketHolder customerTicketHolder,
            CustomerDecisionLogic customerDecisionLogic,
            Action onMoneyChanged)
        {
            owner = customerOwner;
            profile = customerProfile;
            stats = runtimeStats;
            slotMachineManager = machineManager;
            ticketHolder = customerTicketHolder;
            decisionLogic = customerDecisionLogic;
            notifyMoneyChanged = onMoneyChanged;
        }

        public CustomerSlotSearchResult TryFindAndReserveMachine(out SlotMachine machine)
        {
            machine = null;
            reservedSlotMachine = null;

            if (slotMachineManager == null)
            {
                return CustomerSlotSearchResult.NoMachineAvailable;
            }

            bool foundSlot = slotMachineManager.TryGetAvailableMachine(out reservedSlotMachine);

            if (!foundSlot || reservedSlotMachine == null)
            {
                reservedSlotMachine = null;
                return CustomerSlotSearchResult.NoMachineAvailable;
            }

            bool reserved = reservedSlotMachine.TryReserve(owner);

            if (!reserved)
            {
                reservedSlotMachine = null;
                return CustomerSlotSearchResult.ReservationFailed;
            }

            machine = reservedSlotMachine;
            return CustomerSlotSearchResult.Reserved;
        }

        public IEnumerator PlayReservedSlot()
        {
            if (reservedSlotMachine == null)
            {
                yield break;
            }

            bool keepPlayingThisMachine = true;

            while (keepPlayingThisMachine && stats.WalletBalance > 0)
            {
                currentSlotMachineBecameUnavailable = false;
                pendingMachineUnavailableTicketAmount = 0;

                int depositAmount = profile.GetRandomDepositAmount(stats.WalletBalance);

                if (depositAmount <= 0)
                {
                    stats.SetMood(CustomerMood.Broke);
                    yield break;
                }

                int walletBeforeSession = stats.WalletBalance;

                stats.SpendFromWallet(depositAmount);
                notifyMoneyChanged?.Invoke();

                bool sessionStarted = reservedSlotMachine.TryBeginSession(owner, depositAmount);

                if (!sessionStarted)
                {
                    stats.AddCashOutPaymentToWallet(depositAmount);
                    notifyMoneyChanged?.Invoke();

                    reservedSlotMachine.ReleaseReservation(owner);
                    yield break;
                }

                decisionLogic.UpdateMoodFromCurrentMoney(reservedSlotMachine.MachineCredit);

                int spinsThisDepositSession = 0;

                while (reservedSlotMachine != null &&
                       reservedSlotMachine.MachineCredit > 0 &&
                       !currentSlotMachineBecameUnavailable)
                {
                    SlotSpinResult result = reservedSlotMachine.Spin();

                    if (!result.HasBet)
                    {
                        break;
                    }

                    spinsThisDepositSession++;

                    stats.RecordSpin(result.BetAmount, result.PayoutAmount);
                    decisionLogic.UpdateMoodFromCurrentMoney(reservedSlotMachine.MachineCredit);
                    notifyMoneyChanged?.Invoke();

                    if (logEverySpin)
                    {
                        Debug.Log(
                            $"{name} spun {reservedSlotMachine.name}. Bet: £{result.BetAmount}, Payout: £{result.PayoutAmount}, Credit: £{result.RemainingCredit}, Session Spins: {spinsThisDepositSession}, Wallet: £{stats.WalletBalance}, Ticket: £{ticketHolder.ActiveTicketValue}, Total Money: £{stats.GetTotalMoney(reservedSlotMachine.MachineCredit, ticketHolder.ActiveTicketValue)}, Mood: {stats.Mood}.",
                            reservedSlotMachine);
                    }

                    float spinDelay = slotMachineManager != null
                        ? slotMachineManager.SpinDurationSeconds
                        : 1f;

                    yield return new WaitForSeconds(spinDelay);

                    if (currentSlotMachineBecameUnavailable)
                    {
                        break;
                    }

                    if (!CanCashOutCurrentSlotCredit())
                    {
                        continue;
                    }

                    if (spinsThisDepositSession >= profile.MaximumSessionSpins)
                    {
                        break;
                    }

                    if (decisionLogic.ShouldQuitCurrentMachine(
                            CanCashOutCurrentSlotCredit(),
                            spinsThisDepositSession))
                    {
                        break;
                    }
                }

                int ticketAmount = 0;

                if (currentSlotMachineBecameUnavailable)
                {
                    ticketAmount = Mathf.Max(0, pendingMachineUnavailableTicketAmount);

                    if (ticketAmount > 0)
                    {
                        ticketHolder.CreateHeldTicket(ticketAmount);

                        Debug.Log(
                            $"{name} received a forced cash-out ticket for £{ticketAmount} because their slot machine was moved.",
                            this);
                    }

                    pendingMachineUnavailableTicketAmount = 0;
                    currentSlotMachineBecameUnavailable = false;
                    keepPlayingThisMachine = false;

                    if (reservedSlotMachine != null && reservedSlotMachine.IsBeingMovedForPlacement)
                    {
                        reservedSlotMachine = null;
                    }
                }
                else if (CanCashOutCurrentSlotCredit())
                {
                    ticketAmount = reservedSlotMachine.PrintTicketAndEndSession(owner);
                }
                else
                {
                    reservedSlotMachine.EndSessionWithoutTicket(owner);
                }

                if (ticketAmount > 0 && !ticketHolder.HasTicket)
                {
                    ticketHolder.CreateHeldTicket(ticketAmount);
                }

                decisionLogic.UpdateMoodFromCurrentMoney(0);
                notifyMoneyChanged?.Invoke();

                if (logSessionSummary)
                {
                    int sessionDeltaBeforeTicketPayment =
                        stats.WalletBalance + ticketHolder.ActiveTicketValue - walletBeforeSession;

                    Debug.Log(
                        $"{name} finished slot deposit session on {(reservedSlotMachine != null ? reservedSlotMachine.name : "Unavailable Slot Machine")}. Deposit: £{depositAmount}, Ticket: £{ticketAmount}, Session Result Before Cash Desk: £{sessionDeltaBeforeTicketPayment}, Wallet: £{stats.WalletBalance}, Ticket Held: £{ticketHolder.ActiveTicketValue}, Starting Balance: £{stats.StartingBalance}, Mood: {stats.Mood}.",
                        this);
                }

                if (ticketHolder.HasTicket)
                {
                    keepPlayingThisMachine = false;
                    continue;
                }

                if (stats.WalletBalance <= 0)
                {
                    stats.SetMood(CustomerMood.Broke);
                    keepPlayingThisMachine = false;
                    continue;
                }

                if (reservedSlotMachine == null)
                {
                    keepPlayingThisMachine = false;
                    continue;
                }

                bool slotCreditHitZero = ticketAmount <= 0;

                if (!slotCreditHitZero)
                {
                    keepPlayingThisMachine = false;
                    continue;
                }

                bool shouldRedeposit = UnityEngine.Random.value < profile.RedepositChanceWhenSlotCreditHitsZero;

                if (!shouldRedeposit)
                {
                    if (logSessionSummary)
                    {
                        Debug.Log($"{name}'s slot credit hit £0 and they chose not to redeposit.", this);
                    }

                    keepPlayingThisMachine = false;
                    continue;
                }

                if (logSessionSummary)
                {
                    Debug.Log($"{name}'s slot credit hit £0 and they chose to redeposit.", this);
                }

                yield return new WaitForSeconds(profile.GetRandomIdleWaitSeconds());
            }
        }

        public void NotifySlotMachineBecameUnavailable(SlotMachine slotMachine, int ticketAmount)
        {
            if (slotMachine == null)
            {
                return;
            }

            if (reservedSlotMachine != slotMachine)
            {
                return;
            }

            currentSlotMachineBecameUnavailable = true;
            pendingMachineUnavailableTicketAmount += Mathf.Max(0, ticketAmount);

            Debug.Log(
                $"{name}'s slot machine became unavailable. Pending forced ticket: £{pendingMachineUnavailableTicketAmount}.",
                this);
        }

        public void ReleaseReservation()
        {
            if (reservedSlotMachine == null)
            {
                return;
            }

            reservedSlotMachine.ReleaseReservation(owner);
            reservedSlotMachine = null;
        }

        public void ClearReservedSlotReference()
        {
            reservedSlotMachine = null;
        }

        private bool CanCashOutCurrentSlotCredit()
        {
            if (reservedSlotMachine == null)
            {
                return false;
            }

            return reservedSlotMachine.MachineCredit >= profile.MinimumTicketCashOutAmount;
        }
    }
}