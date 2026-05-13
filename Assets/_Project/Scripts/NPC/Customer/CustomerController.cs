using System.Collections;
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
            Leaving,
            Finished
        }

        [Header("Navigation")]
        [SerializeField, Min(0.05f)] private float destinationReachedDistance = 0.35f;
        [SerializeField, Min(0.1f)] private float searchRetryDelay = 1f;

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
        private Vector3 exitPosition;
        private bool wantsToLeaveCasino;

        private int lifetimeSpinCountWhenSatisfiedStarted = -1;

        public CustomerRuntimeStats Stats => stats;
        public string CurrentStateName => currentState.ToString();

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

            name = $"Customer_{profile.Stake}_${stats.StartingBalance}";

            Debug.Log(
                $"{name} spawned. Stake: {stats.Stake}, Starting Balance: £{stats.StartingBalance}, Mood: {stats.Mood}.",
                this);

            StartCoroutine(CustomerRoutine());
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

                if (ShouldLeaveCasinoAfterSession())
                {
                    wantsToLeaveCasino = true;
                    break;
                }

                reservedSlotMachine = null;

                if (Random.value < profile.WanderAfterMachineChance)
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
                int depositAmount = profile.GetRandomDepositAmount(stats.WalletBalance);

                if (depositAmount <= 0)
                {
                    stats.SetMood(CustomerMood.Broke);
                    yield break;
                }

                int walletBeforeSession = stats.WalletBalance;

                stats.SpendFromWallet(depositAmount);

                bool sessionStarted = reservedSlotMachine.TryBeginSession(this, depositAmount);

                if (!sessionStarted)
                {
                    stats.AddToWallet(depositAmount);
                    reservedSlotMachine.ReleaseReservation(this);
                    yield break;
                }

                UpdateMoodFromCurrentMoney(reservedSlotMachine.MachineCredit);

                int spinsThisDepositSession = 0;

                while (reservedSlotMachine.MachineCredit > 0 &&
                       spinsThisDepositSession < profile.MaximumSessionSpins)
                {
                    SlotSpinResult result = reservedSlotMachine.Spin();

                    if (!result.HasBet)
                    {
                        break;
                    }

                    spinsThisDepositSession++;

                    stats.RecordSpin(result.BetAmount, result.PayoutAmount);
                    UpdateMoodFromCurrentMoney(reservedSlotMachine.MachineCredit);

                    if (logEverySpin)
                    {
                        Debug.Log(
                            $"{name} spun {reservedSlotMachine.name}. Bet: £{result.BetAmount}, Payout: £{result.PayoutAmount}, Credit: £{result.RemainingCredit}, Session Spins: {spinsThisDepositSession}, Wallet: £{stats.WalletBalance}, Total Money: £{stats.GetTotalMoney(reservedSlotMachine.MachineCredit)}, Mood: {stats.Mood}.",
                            reservedSlotMachine);
                    }

                    float spinDelay = slotMachineManager != null
                        ? slotMachineManager.SpinDurationSeconds
                        : 1f;

                    yield return new WaitForSeconds(spinDelay);

                    if (ShouldQuitCurrentMachine(spinsThisDepositSession))
                    {
                        break;
                    }
                }

                int withdrawnAmount = reservedSlotMachine.EndSession(this);

                if (withdrawnAmount > 0)
                {
                    stats.AddToWallet(withdrawnAmount);
                }

                UpdateMoodFromCurrentMoney(0);

                if (logSessionSummary)
                {
                    int sessionProfit = stats.WalletBalance - walletBeforeSession;

                    Debug.Log(
                        $"{name} finished slot deposit session on {reservedSlotMachine.name}. Deposit: £{depositAmount}, Withdrawn: £{withdrawnAmount}, Session Profit: £{sessionProfit}, Wallet: £{stats.WalletBalance}, Starting Balance: £{stats.StartingBalance}, Mood: {stats.Mood}.",
                        this);
                }

                if (stats.WalletBalance <= 0)
                {
                    stats.SetMood(CustomerMood.Broke);
                    keepPlayingThisMachine = false;
                    continue;
                }

                bool slotCreditHitZero = withdrawnAmount <= 0;

                if (!slotCreditHitZero)
                {
                    keepPlayingThisMachine = false;
                    continue;
                }

                bool shouldRedeposit = Random.value < profile.RedepositChanceWhenSlotCreditHitsZero;

                if (!shouldRedeposit)
                {
                    if (logSessionSummary)
                    {
                        Debug.Log(
                            $"{name}'s slot credit hit £0 and they chose not to redeposit.",
                            this);
                    }

                    keepPlayingThisMachine = false;
                    continue;
                }

                if (logSessionSummary)
                {
                    Debug.Log(
                        $"{name}'s slot credit hit £0 and they chose to redeposit.",
                        this);
                }

                yield return new WaitForSeconds(profile.GetRandomIdleWaitSeconds());
            }
        }

        private IEnumerator WaitInsideCasino()
        {
            bool shouldStandStill = Random.value < profile.StandStillWhileWaitingChance;

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

            return roamPoints[Random.Range(0, roamPoints.Length)];
        }

        private void UpdateMoodFromCurrentMoney(int activeSlotCredit)
        {
            CustomerMood previousMood = stats.Mood;

            int totalMoney = stats.GetTotalMoney(activeSlotCredit);
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
            if (spinsThisDepositSession < profile.MinimumSessionSpinsBeforeLeaving)
            {
                return false;
            }

            return stats.Mood switch
            {
                CustomerMood.Happy => false,
                CustomerMood.Satisfied => HasSatisfiedTestSpinsCompleted() && Random.value < profile.SatisfiedLeaveChance,
                CustomerMood.Frustrated => Random.value < profile.FrustratedLeaveChance,
                CustomerMood.Angry => Random.value < profile.AngryLeaveChance,
                CustomerMood.Broke => true,
                _ => false
            };
        }

        private bool ShouldLeaveCasinoAfterSession()
        {
            return stats.Mood switch
            {
                CustomerMood.Happy => false,
                CustomerMood.Satisfied => HasSatisfiedTestSpinsCompleted() && Random.value < profile.SatisfiedLeaveChance,
                CustomerMood.Frustrated => Random.value < profile.FrustratedLeaveChance,
                CustomerMood.Angry => Random.value < profile.AngryLeaveChance,
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

        private void OnDestroy()
        {
            if (reservedSlotMachine != null)
            {
                reservedSlotMachine.ReleaseReservation(this);
            }
        }
    }
}