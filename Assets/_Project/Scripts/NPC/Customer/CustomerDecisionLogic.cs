using UnityEngine;

namespace Project.NPC.Customer
{
    public sealed class CustomerDecisionLogic : MonoBehaviour
    {
        private CustomerProfileSO profile;
        private CustomerRuntimeStats stats;
        private CustomerTicketHolder ticketHolder;
        private bool logSessionSummary;

        private int lifetimeSpinCountWhenSatisfiedStarted = -1;

        public void Initialize(
            CustomerProfileSO customerProfile,
            CustomerRuntimeStats runtimeStats,
            CustomerTicketHolder customerTicketHolder,
            bool shouldLogSessionSummary)
        {
            profile = customerProfile;
            stats = runtimeStats;
            ticketHolder = customerTicketHolder;
            logSessionSummary = shouldLogSessionSummary;
        }

        public void UpdateMoodFromCurrentMoney(int activeSlotCredit)
        {
            if (profile == null || stats == null || ticketHolder == null)
            {
                return;
            }

            CustomerMood previousMood = stats.Mood;

            int totalMoney = stats.GetTotalMoney(activeSlotCredit, ticketHolder.ActiveTicketValue);
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

        public bool ShouldQuitCurrentMachine(bool canCashOutCurrentSlotCredit, int spinsThisDepositSession)
        {
            if (profile == null || stats == null)
            {
                return false;
            }

            if (!canCashOutCurrentSlotCredit)
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
                CustomerMood.Satisfied => HasSatisfiedTestSpinsCompleted() &&
                                          UnityEngine.Random.value < profile.SatisfiedLeaveChance,
                CustomerMood.Frustrated => UnityEngine.Random.value < profile.FrustratedLeaveChance,
                CustomerMood.Angry => UnityEngine.Random.value < profile.AngryLeaveChance,
                CustomerMood.Broke => true,
                _ => false
            };
        }

        public bool ShouldLeaveCasinoAfterSession(bool hasCompletedCashDeskCashOut)
        {
            if (profile == null || stats == null)
            {
                return false;
            }

            if (hasCompletedCashDeskCashOut)
            {
                return true;
            }

            return stats.Mood switch
            {
                CustomerMood.Happy => false,
                CustomerMood.Satisfied => HasSatisfiedTestSpinsCompleted() &&
                                          UnityEngine.Random.value < profile.SatisfiedLeaveChance,
                CustomerMood.Frustrated => UnityEngine.Random.value < profile.FrustratedLeaveChance,
                CustomerMood.Angry => UnityEngine.Random.value < profile.AngryLeaveChance,
                CustomerMood.Broke => true,
                _ => false
            };
        }

        private bool HasSatisfiedTestSpinsCompleted()
        {
            if (profile == null || stats == null)
            {
                return true;
            }

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
    }
}