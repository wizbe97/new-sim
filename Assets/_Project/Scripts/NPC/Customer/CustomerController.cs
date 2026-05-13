using System;
using System.Collections;
using Project.CashDesk;
using Project.Managers;
using Project.SlotMachines;
using Project.World;
using UnityEngine;
using UnityEngine.AI;

namespace Project.NPC.Customer
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CustomerController : MonoBehaviour
    {
        private enum CustomerState
        {
            Uninitialized,
            GoingToEntrance,
            LookingForSlot,
            GoingToSlot,
            PlayingSlot,
            Wandering,
            StandingIdle,
            GoingToCashDesk,
            WaitingAtCashDesk,
            Leaving,
            Finished
        }

        [Header("Navigation")]
        [SerializeField, Min(0.05f)] private float destinationReachedDistance = 0.35f;
        [SerializeField, Min(0.1f)] private float searchRetryDelay = 1f;

        [Header("Cash Out Ticket")]
        [SerializeField] private Vector3 ticketHandLocalPosition = new Vector3(0.35f, 0.6f, 0.25f);
        [SerializeField] private Vector3 ticketHandLocalEulerAngles = Vector3.zero;

        [Header("Debug Logging")]
        [SerializeField] private bool logEverySpin = true;
        [SerializeField] private bool logSessionSummary = true;
        [SerializeField] private bool logIdleBehaviour = true;

        [Header("Runtime Debug")]
        [SerializeField] private CustomerState currentState = CustomerState.Uninitialized;
        [SerializeField] private CustomerRuntimeStats stats;

        private CustomerProfileSO profile;
        private NavMeshAgent agent;
        private CasinoEntrance entrance;
        private SlotMachineManager slotMachineManager;
        private SlotMachine reservedSlotMachine;
        private CasinoRoamPoint[] roamPoints;
        private CashDeskManager cashDeskManager;
        private CashOutTicket heldTicket;
        private Transform ticketHandPoint;

        private Vector3 activeQueueTargetPosition;
        private Vector3 activeQueueFacingPosition;
        private bool hasActiveQueueTarget;
        private bool isFrontOfCashDeskQueue;
        private Transform activeTicketPlacementPoint;

        private Vector3 exitPosition;
        private bool wantsToLeaveCasino;
        private bool isWaitingForCashDeskPayment;
        private bool hasPlacedTicketOnDesk;
        private bool hasCompletedCashDeskCashOut;

        private int lifetimeSpinCountWhenSatisfiedStarted = -1;

        public event Action<CustomerController> CustomerMoneyChanged;
        public event Action<int> CashOutPaymentReceived;

        public CustomerRuntimeStats Stats => stats;
        public string CurrentStateName => currentState.ToString();
        public int ActiveTicketValue => heldTicket != null && !heldTicket.IsPaid ? heldTicket.Amount : 0;
        private bool currentSlotMachineBecameUnavailable;
        private int pendingMachineUnavailableTicketAmount;

        public void Initialize(
            CustomerProfileSO customerProfile,
            CasinoEntrance casinoEntrance,
            SlotMachineManager machineManager,
            Vector3 leavePosition)
        {
            if (customerProfile == null)
            {
                Debug.LogError($"{nameof(CustomerController)} cannot initialize without a profile.", this);
                return;
            }

            if (casinoEntrance == null)
            {
                Debug.LogError($"{nameof(CustomerController)} cannot initialize without a casino entrance.", this);
                return;
            }

            if (machineManager == null)
            {
                Debug.LogError($"{nameof(CustomerController)} cannot initialize without a SlotMachineManager.", this);
                return;
            }

            profile = customerProfile;
            entrance = casinoEntrance;
            slotMachineManager = machineManager;
            exitPosition = leavePosition;

            stats = new CustomerRuntimeStats(
                profile.Stake,
                profile.GetRandomStartingBalance());

            agent = GetComponent<NavMeshAgent>();
            roamPoints = FindObjectsByType<CasinoRoamPoint>(FindObjectsSortMode.None);
            cashDeskManager = FindFirstObjectByType<CashDeskManager>();

            CreateTicketHandPoint();

            name = $"Customer_{profile.Stake}_£{stats.StartingBalance}";

            Debug.Log(
                $"{name} spawned. Stake: {stats.Stake}, Starting Balance: £{stats.StartingBalance}, Mood: {stats.Mood}.",
                this);

            NotifyMoneyChanged();

            StartCoroutine(CustomerRoutine());
        }

        private void Update()
        {
            UpdateCashDeskQueueMovement();
        }

        private IEnumerator CustomerRoutine()
        {
            currentState = CustomerState.GoingToEntrance;

            yield return MoveTo(entrance.Position);

            int noMachineWaitingActions = 0;

            while (!wantsToLeaveCasino && stats.WalletBalance > 0)
            {
                currentState = CustomerState.LookingForSlot;

                bool foundSlot = slotMachineManager.TryGetAvailableMachine(out reservedSlotMachine);

                if (!foundSlot || reservedSlotMachine == null)
                {
                    noMachineWaitingActions++;

                    if (logIdleBehaviour)
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

                    yield return WaitInsideCasino();
                    yield return new WaitForSeconds(searchRetryDelay);
                    continue;
                }

                bool reserved = reservedSlotMachine.TryReserve(this);

                if (!reserved)
                {
                    reservedSlotMachine = null;

                    yield return WaitInsideCasino();
                    yield return new WaitForSeconds(searchRetryDelay);
                    continue;
                }

                noMachineWaitingActions = 0;

                currentState = CustomerState.GoingToSlot;

                yield return MoveTo(reservedSlotMachine.PlayPoint.position);

                currentState = CustomerState.PlayingSlot;

                yield return PlayReservedSlot();

                if (heldTicket != null)
                {
                    yield return CashOutHeldTicket();
                }

                if (hasCompletedCashDeskCashOut)
                {
                    wantsToLeaveCasino = true;
                    break;
                }

                if (ShouldLeaveCasinoAfterSession())
                {
                    wantsToLeaveCasino = true;
                    break;
                }

                reservedSlotMachine = null;

                if (UnityEngine.Random.value < profile.WanderAfterMachineChance)
                {
                    yield return WaitInsideCasino();
                }
            }

            UpdateMoodFromCurrentMoney(0);

            yield return LeaveCasino();
        }

        private IEnumerator PlayReservedSlot()
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
                NotifyMoneyChanged();

                bool sessionStarted = reservedSlotMachine.TryBeginSession(this, depositAmount);

                if (!sessionStarted)
                {
                    stats.AddCashOutPaymentToWallet(depositAmount);
                    NotifyMoneyChanged();
                    reservedSlotMachine.ReleaseReservation(this);
                    yield break;
                }

                UpdateMoodFromCurrentMoney(reservedSlotMachine.MachineCredit);

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
                    UpdateMoodFromCurrentMoney(reservedSlotMachine.MachineCredit);
                    NotifyMoneyChanged();

                    if (logEverySpin)
                    {
                        Debug.Log(
                            $"{name} spun {reservedSlotMachine.name}. Bet: £{result.BetAmount}, Payout: £{result.PayoutAmount}, Credit: £{result.RemainingCredit}, Session Spins: {spinsThisDepositSession}, Wallet: £{stats.WalletBalance}, Ticket: £{ActiveTicketValue}, Total Money: £{stats.GetTotalMoney(reservedSlotMachine.MachineCredit, ActiveTicketValue)}, Mood: {stats.Mood}.",
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

                    if (ShouldQuitCurrentMachine(spinsThisDepositSession))
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
                        CreateHeldTicket(ticketAmount);

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
                    ticketAmount = reservedSlotMachine.PrintTicketAndEndSession(this);
                }
                else
                {
                    reservedSlotMachine.EndSessionWithoutTicket(this);
                }

                if (ticketAmount > 0 && heldTicket == null)
                {
                    CreateHeldTicket(ticketAmount);
                }

                UpdateMoodFromCurrentMoney(0);
                NotifyMoneyChanged();

                if (logSessionSummary)
                {
                    int sessionDeltaBeforeTicketPayment = stats.WalletBalance + ActiveTicketValue - walletBeforeSession;

                    Debug.Log(
                        $"{name} finished slot deposit session on {(reservedSlotMachine != null ? reservedSlotMachine.name : "Unavailable Slot Machine")}. Deposit: £{depositAmount}, Ticket: £{ticketAmount}, Session Result Before Cash Desk: £{sessionDeltaBeforeTicketPayment}, Wallet: £{stats.WalletBalance}, Ticket Held: £{ActiveTicketValue}, Starting Balance: £{stats.StartingBalance}, Mood: {stats.Mood}.",
                        this);
                }

                if (heldTicket != null)
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

        private bool CanCashOutCurrentSlotCredit()
        {
            if (reservedSlotMachine == null)
            {
                return false;
            }

            return reservedSlotMachine.MachineCredit >= profile.MinimumTicketCashOutAmount;
        }

        private IEnumerator CashOutHeldTicket()
        {
            if (heldTicket == null)
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

            currentState = CustomerState.GoingToCashDesk;
            isWaitingForCashDeskPayment = true;
            hasPlacedTicketOnDesk = false;
            hasActiveQueueTarget = false;
            isFrontOfCashDeskQueue = false;

            cashDeskManager.QueueCustomer(this, heldTicket);

            while (isWaitingForCashDeskPayment && heldTicket != null)
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

            if (agent != null)
            {
                agent.SetDestination(activeQueueTargetPosition);
            }

            currentState = CustomerState.WaitingAtCashDesk;
        }

        public void ReceiveCashOutPayment(int amount)
        {
            stats.AddCashOutPaymentToWallet(amount);

            heldTicket = null;

            isWaitingForCashDeskPayment = false;
            hasPlacedTicketOnDesk = false;
            hasActiveQueueTarget = false;
            isFrontOfCashDeskQueue = false;
            activeTicketPlacementPoint = null;

            hasCompletedCashDeskCashOut = true;
            wantsToLeaveCasino = true;

            UpdateMoodFromCurrentMoney(0);
            NotifyMoneyChanged();

            CashOutPaymentReceived?.Invoke(amount);

            BeginImmediateExitMovement();
        }

        private void BeginImmediateExitMovement()
        {
            currentState = CustomerState.Leaving;

            if (agent == null || !agent.enabled)
            {
                return;
            }

            agent.ResetPath();
            agent.SetDestination(exitPosition);
        }

        private void UpdateCashDeskQueueMovement()
        {
            if (!isWaitingForCashDeskPayment || !hasActiveQueueTarget)
            {
                return;
            }

            if (agent == null || agent.pathPending)
            {
                return;
            }

            if (agent.remainingDistance > destinationReachedDistance)
            {
                return;
            }

            FaceCashDeskQueueTarget();

            if (isFrontOfCashDeskQueue)
            {
                TryPlaceTicketOnDeskWhenReady();
            }
        }

        private void FaceCashDeskQueueTarget()
        {
            Vector3 direction = activeQueueFacingPosition - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void TryPlaceTicketOnDeskWhenReady()
        {
            if (hasPlacedTicketOnDesk || heldTicket == null || activeTicketPlacementPoint == null)
            {
                return;
            }

            heldTicket.PlaceOnDesk(activeTicketPlacementPoint);
            hasPlacedTicketOnDesk = true;

            Debug.Log($"{name} placed a £{heldTicket.Amount} ticket on the cash desk.", this);
        }

        private void CreateHeldTicket(int ticketAmount)
        {
            if (ticketAmount <= 0 || ticketHandPoint == null)
            {
                return;
            }

            heldTicket = CashOutTicket.Create(ticketAmount, this, ticketHandPoint);
            heldTicket.AttachToHand(ticketHandPoint);

            Debug.Log($"{name} printed and is holding a £{ticketAmount} cash-out ticket.", this);
        }

        private IEnumerator WaitInsideCasino()
        {
            bool shouldStandStill = UnityEngine.Random.value < profile.StandStillWhileWaitingChance;

            if (shouldStandStill || roamPoints == null || roamPoints.Length == 0)
            {
                currentState = CustomerState.StandingIdle;

                if (logIdleBehaviour)
                {
                    Debug.Log($"{name} is standing idle inside the casino.", this);
                }

                yield return new WaitForSeconds(profile.GetRandomIdleWaitSeconds());
                yield break;
            }

            CasinoRoamPoint roamPoint = GetRandomRoamPoint();

            if (roamPoint == null)
            {
                yield return new WaitForSeconds(profile.GetRandomIdleWaitSeconds());
                yield break;
            }

            currentState = CustomerState.Wandering;

            if (logIdleBehaviour)
            {
                Debug.Log($"{name} is wandering to {roamPoint.name}.", roamPoint);
            }

            yield return MoveTo(roamPoint.Position);

            currentState = CustomerState.StandingIdle;

            yield return new WaitForSeconds(profile.GetRandomIdleWaitSeconds());
        }

        private CasinoRoamPoint GetRandomRoamPoint()
        {
            if (roamPoints == null || roamPoints.Length == 0)
            {
                return null;
            }

            return roamPoints[UnityEngine.Random.Range(0, roamPoints.Length)];
        }

        private void UpdateMoodFromCurrentMoney(int activeSlotCredit)
        {
            CustomerMood previousMood = stats.Mood;

            int totalMoney = stats.GetTotalMoney(activeSlotCredit, ActiveTicketValue);
            CustomerMood newMood = profile.GetMoodForTotalMoney(stats.StartingBalance, totalMoney);

            stats.SetMood(newMood);

            if (previousMood != CustomerMood.Satisfied && newMood == CustomerMood.Satisfied)
            {
                lifetimeSpinCountWhenSatisfiedStarted = stats.SpinsPlayed;

                if (logSessionSummary)
                {
                    Debug.Log(
                        $"{name} became Satisfied at £{totalMoney}. They will test spin for at least {profile.MinimumSpinsAfterBecomingSatisfiedBeforeLeaving} more spins before leaving because of satisfaction.",
                        this);
                }
            }

            if (previousMood == CustomerMood.Satisfied && newMood != CustomerMood.Satisfied)
            {
                lifetimeSpinCountWhenSatisfiedStarted = -1;
            }
        }

        private bool HasSatisfiedTestSpinsCompleted()
        {
            if (stats.Mood != CustomerMood.Satisfied)
            {
                return true;
            }

            if (lifetimeSpinCountWhenSatisfiedStarted < 0)
            {
                return profile.MinimumSpinsAfterBecomingSatisfiedBeforeLeaving <= 0;
            }

            int spinsSinceSatisfied = stats.SpinsPlayed - lifetimeSpinCountWhenSatisfiedStarted;

            return spinsSinceSatisfied >= profile.MinimumSpinsAfterBecomingSatisfiedBeforeLeaving;
        }

        private bool ShouldQuitCurrentMachine(int spinsThisDepositSession)
        {
            if (!CanCashOutCurrentSlotCredit())
            {
                return false;
            }

            if (spinsThisDepositSession < profile.MinimumSessionSpinsBeforeLeaving)
            {
                return false;
            }

            return stats.Mood switch
            {
                CustomerMood.Happy => false,
                CustomerMood.Satisfied => HasSatisfiedTestSpinsCompleted() && UnityEngine.Random.value < profile.SatisfiedLeaveChance,
                CustomerMood.Frustrated => UnityEngine.Random.value < profile.FrustratedLeaveChance,
                CustomerMood.Angry => UnityEngine.Random.value < profile.AngryLeaveChance,
                CustomerMood.Broke => true,
                _ => false
            };
        }

        private bool ShouldLeaveCasinoAfterSession()
        {
            if (hasCompletedCashDeskCashOut)
            {
                return true;
            }

            return stats.Mood switch
            {
                CustomerMood.Happy => false,
                CustomerMood.Satisfied => HasSatisfiedTestSpinsCompleted() && UnityEngine.Random.value < profile.SatisfiedLeaveChance,
                CustomerMood.Frustrated => UnityEngine.Random.value < profile.FrustratedLeaveChance,
                CustomerMood.Angry => UnityEngine.Random.value < profile.AngryLeaveChance,
                CustomerMood.Broke => true,
                _ => false
            };
        }

        private IEnumerator LeaveCasino()
        {
            currentState = CustomerState.Leaving;

            if (reservedSlotMachine != null)
            {
                reservedSlotMachine.ReleaseReservation(this);
                reservedSlotMachine = null;
            }

            Debug.Log(
                $"{name} is leaving casino. Starting Balance: £{stats.StartingBalance}, Final Wallet: £{stats.WalletBalance}, Net Result: £{stats.WalletBalance - stats.StartingBalance}, Mood: {stats.Mood}, Lifetime Spins: {stats.SpinsPlayed}.",
                this);

            yield return MoveTo(exitPosition);

            currentState = CustomerState.Finished;

            Destroy(gameObject);
        }

        private IEnumerator MoveTo(Vector3 destination)
        {
            if (agent == null)
            {
                yield break;
            }

            bool destinationSet = agent.SetDestination(destination);

            if (!destinationSet)
            {
                Debug.LogWarning($"{name} could not path to destination {destination}.", this);
                yield break;
            }

            while (agent.pathPending)
            {
                yield return null;
            }

            while (agent.enabled && agent.remainingDistance > destinationReachedDistance)
            {
                yield return null;
            }
        }

        private void CreateTicketHandPoint()
        {
            GameObject handPointObject = new GameObject("TicketHandPoint");
            ticketHandPoint = handPointObject.transform;
            ticketHandPoint.SetParent(transform, false);
            ticketHandPoint.localPosition = ticketHandLocalPosition;
            ticketHandPoint.localRotation = Quaternion.Euler(ticketHandLocalEulerAngles);
        }

        private void NotifyMoneyChanged()
        {
            CustomerMoneyChanged?.Invoke(this);
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

        private void OnDestroy()
        {
            if (reservedSlotMachine != null)
            {
                reservedSlotMachine.ReleaseReservation(this);
            }
        }
    }
}