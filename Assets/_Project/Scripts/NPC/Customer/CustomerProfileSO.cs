using UnityEngine;

namespace Project.NPC.Customer
{
    [CreateAssetMenu(
        fileName = "CustomerProfile",
        menuName = "Project/NPC/Customer Profile")]
    public sealed class CustomerProfileSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string profileName = "Low Roller Customer";

        [Header("Stake")]
        [SerializeField] private CustomerStake stake = CustomerStake.LowRoller;

        [Header("Starting Balance")]
        [SerializeField, Min(0)] private int lowRollerMinBalance = 50;
        [SerializeField, Min(0)] private int lowRollerMaxBalance = 200;

        [SerializeField, Min(0)] private int mediumRollerMinBalance = 200;
        [SerializeField, Min(0)] private int mediumRollerMaxBalance = 1000;

        [SerializeField, Min(0)] private int highRollerMinBalance = 1000;
        [SerializeField, Min(0)] private int highRollerMaxBalance = 5000;

        [Header("Money Behaviour")]
        [Tooltip("Minimum percentage of their current wallet balance they deposit into a slot.")]
        [SerializeField, Range(0.01f, 1f)] private float minDepositPercent = 0.25f;

        [Tooltip("Maximum percentage of their current wallet balance they deposit into a slot.")]
        [SerializeField, Range(0.01f, 1f)] private float maxDepositPercent = 1f;

        [Tooltip("Chance that a customer deposits again when their current slot credit reaches £0.")]
        [SerializeField, Range(0f, 1f)] private float redepositChanceWhenSlotCreditHitsZero = 0.5f;

        [Tooltip("Customers will not voluntarily print/cash out a ticket below this value. This prevents tiny £1/£2 tickets.")]
        [SerializeField, Min(0)] private int minimumTicketCashOutAmount = 10;

        [Header("Mood Balance Thresholds")]
        [Tooltip("Customer becomes Satisfied when their total money is at least this multiplier of their starting balance.")]
        [SerializeField, Min(1f)] private float satisfiedBalanceMultiplier = 1.5f;

        [Tooltip("Customer becomes Frustrated when their total money is at or below this multiplier of their starting balance.")]
        [SerializeField, Range(0f, 1f)] private float frustratedBalanceMultiplier = 0.5f;

        [Tooltip("Customer becomes Angry when their total money is at or below this multiplier of their starting balance.")]
        [SerializeField, Range(0f, 1f)] private float angryBalanceMultiplier = 0.2f;

        [Header("Session Length")]
        [Tooltip("Customers will not voluntarily leave a machine until they have played at least this many spins in the current deposit session, unless their slot credit hits £0.")]
        [SerializeField, Min(1)] private int minimumSessionSpinsBeforeLeaving = 25;

        [Tooltip("After becoming Satisfied, customers must play this many extra spins before they are allowed to cash out because they are satisfied.")]
        [SerializeField, Min(0)] private int minimumSpinsAfterBecomingSatisfiedBeforeLeaving = 5;

        [Tooltip("Hard cap for one deposit session. If the customer has less than Minimum Ticket Cash Out Amount, they keep spinning until they either bust or rise above the minimum cash-out amount.")]
        [SerializeField, Min(1)] private int maximumSessionSpins = 80;

        [Header("Leave Chances")]
        [Tooltip("Chance to leave after a session when Satisfied, once they have done their extra satisfied test spins.")]
        [SerializeField, Range(0f, 1f)] private float satisfiedLeaveChance = 0.45f;

        [Tooltip("Chance to leave after a session when Frustrated.")]
        [SerializeField, Range(0f, 1f)] private float frustratedLeaveChance = 0.35f;

        [Tooltip("Chance to leave after a session when Angry.")]
        [SerializeField, Range(0f, 1f)] private float angryLeaveChance = 0.8f;

        [Header("Idle Casino Behaviour")]
        [Tooltip("Chance that a customer stands still instead of walking to a roam point when waiting.")]
        [SerializeField, Range(0f, 1f)] private float standStillWhileWaitingChance = 0.35f;

        [Tooltip("Minimum seconds to wait when standing or after reaching a roam point.")]
        [SerializeField, Min(0.1f)] private float minIdleWaitSeconds = 2f;

        [Tooltip("Maximum seconds to wait when standing or after reaching a roam point.")]
        [SerializeField, Min(0.1f)] private float maxIdleWaitSeconds = 6f;

        [Tooltip("How many no-machine waiting actions the customer will tolerate before leaving.")]
        [SerializeField, Min(1)] private int maxNoMachineWaitingActionsBeforeLeaving = 5;

        [Tooltip("After leaving a slot machine, chance that the customer wanders before looking for another machine.")]
        [SerializeField, Range(0f, 1f)] private float wanderAfterMachineChance = 0.35f;

        public string ProfileName => profileName;
        public CustomerStake Stake => stake;

        public float MinDepositPercent => minDepositPercent;
        public float MaxDepositPercent => maxDepositPercent;
        public float RedepositChanceWhenSlotCreditHitsZero => redepositChanceWhenSlotCreditHitsZero;
        public int MinimumTicketCashOutAmount => minimumTicketCashOutAmount;

        public float SatisfiedBalanceMultiplier => satisfiedBalanceMultiplier;
        public float FrustratedBalanceMultiplier => frustratedBalanceMultiplier;
        public float AngryBalanceMultiplier => angryBalanceMultiplier;

        public int MinimumSessionSpinsBeforeLeaving => minimumSessionSpinsBeforeLeaving;
        public int MinimumSpinsAfterBecomingSatisfiedBeforeLeaving => minimumSpinsAfterBecomingSatisfiedBeforeLeaving;
        public int MaximumSessionSpins => maximumSessionSpins;

        public float SatisfiedLeaveChance => satisfiedLeaveChance;
        public float FrustratedLeaveChance => frustratedLeaveChance;
        public float AngryLeaveChance => angryLeaveChance;

        public float StandStillWhileWaitingChance => standStillWhileWaitingChance;
        public float MinIdleWaitSeconds => minIdleWaitSeconds;
        public float MaxIdleWaitSeconds => maxIdleWaitSeconds;
        public int MaxNoMachineWaitingActionsBeforeLeaving => maxNoMachineWaitingActionsBeforeLeaving;
        public float WanderAfterMachineChance => wanderAfterMachineChance;

        public int GetRandomStartingBalance()
        {
            return stake switch
            {
                CustomerStake.LowRoller => Random.Range(lowRollerMinBalance, lowRollerMaxBalance + 1),
                CustomerStake.MediumRoller => Random.Range(mediumRollerMinBalance, mediumRollerMaxBalance + 1),
                CustomerStake.HighRoller => Random.Range(highRollerMinBalance, highRollerMaxBalance + 1),
                _ => Random.Range(lowRollerMinBalance, lowRollerMaxBalance + 1)
            };
        }

        public int GetRandomDepositAmount(int currentWalletBalance)
        {
            if (currentWalletBalance <= 0)
            {
                return 0;
            }

            float minPercent = Mathf.Min(minDepositPercent, maxDepositPercent);
            float maxPercent = Mathf.Max(minDepositPercent, maxDepositPercent);

            int minDeposit = Mathf.Max(1, Mathf.FloorToInt(currentWalletBalance * minPercent));
            int maxDeposit = Mathf.Max(minDeposit, Mathf.FloorToInt(currentWalletBalance * maxPercent));

            return Mathf.Clamp(Random.Range(minDeposit, maxDeposit + 1), 1, currentWalletBalance);
        }

        public CustomerMood GetMoodForTotalMoney(int startingBalance, int currentTotalMoney)
        {
            if (currentTotalMoney <= 0)
            {
                return CustomerMood.Broke;
            }

            if (startingBalance <= 0)
            {
                return CustomerMood.Happy;
            }

            float ratio = (float)currentTotalMoney / startingBalance;

            if (ratio >= satisfiedBalanceMultiplier)
            {
                return CustomerMood.Satisfied;
            }

            if (ratio <= angryBalanceMultiplier)
            {
                return CustomerMood.Angry;
            }

            if (ratio <= frustratedBalanceMultiplier)
            {
                return CustomerMood.Frustrated;
            }

            return CustomerMood.Happy;
        }

        public float GetRandomIdleWaitSeconds()
        {
            float min = Mathf.Min(minIdleWaitSeconds, maxIdleWaitSeconds);
            float max = Mathf.Max(minIdleWaitSeconds, maxIdleWaitSeconds);

            return Random.Range(min, max);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (lowRollerMaxBalance < lowRollerMinBalance)
            {
                lowRollerMaxBalance = lowRollerMinBalance;
            }

            if (mediumRollerMaxBalance < mediumRollerMinBalance)
            {
                mediumRollerMaxBalance = mediumRollerMinBalance;
            }

            if (highRollerMaxBalance < highRollerMinBalance)
            {
                highRollerMaxBalance = highRollerMinBalance;
            }

            if (maxDepositPercent < minDepositPercent)
            {
                maxDepositPercent = minDepositPercent;
            }

            if (maximumSessionSpins < minimumSessionSpinsBeforeLeaving)
            {
                maximumSessionSpins = minimumSessionSpinsBeforeLeaving;
            }

            if (maxIdleWaitSeconds < minIdleWaitSeconds)
            {
                maxIdleWaitSeconds = minIdleWaitSeconds;
            }

            frustratedBalanceMultiplier = Mathf.Clamp01(frustratedBalanceMultiplier);
            angryBalanceMultiplier = Mathf.Clamp(angryBalanceMultiplier, 0f, frustratedBalanceMultiplier);
            satisfiedBalanceMultiplier = Mathf.Max(1f, satisfiedBalanceMultiplier);
            minimumTicketCashOutAmount = Mathf.Max(0, minimumTicketCashOutAmount);
        }
#endif
    }
}