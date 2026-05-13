using System;
using UnityEngine;

namespace Project.NPC.Customer
{
    [Serializable]
    public sealed class CustomerRuntimeStats
    {
        [SerializeField] private CustomerStake stake;
        [SerializeField] private CustomerMood mood;
        [SerializeField] private int startingBalance;
        [SerializeField] private int walletBalance;
        [SerializeField] private int totalDeposited;
        [SerializeField] private int totalWithdrawn;
        [SerializeField] private int spinsPlayed;
        [SerializeField] private int wins;
        [SerializeField] private int losses;

        public CustomerStake Stake => stake;
        public CustomerMood Mood => mood;
        public int StartingBalance => startingBalance;
        public int WalletBalance => walletBalance;
        public int TotalDeposited => totalDeposited;
        public int TotalWithdrawn => totalWithdrawn;
        public int SpinsPlayed => spinsPlayed;
        public int Wins => wins;
        public int Losses => losses;

        public CustomerRuntimeStats(CustomerStake stake, int startingBalance)
        {
            this.stake = stake;
            this.startingBalance = Mathf.Max(0, startingBalance);
            walletBalance = this.startingBalance;
            mood = CustomerMood.Happy;
        }

        public int GetTotalMoney(int activeSlotCredit)
        {
            return Mathf.Max(0, walletBalance) + Mathf.Max(0, activeSlotCredit);
        }

        public void SpendFromWallet(int amount)
        {
            int safeAmount = Mathf.Clamp(amount, 0, walletBalance);

            walletBalance -= safeAmount;
            totalDeposited += safeAmount;
        }

        public void AddToWallet(int amount)
        {
            int safeAmount = Mathf.Max(0, amount);

            walletBalance += safeAmount;
            totalWithdrawn += safeAmount;
        }

        public void RecordSpin(int betAmount, int payoutAmount)
        {
            spinsPlayed++;

            if (payoutAmount > betAmount)
            {
                wins++;
            }
            else
            {
                losses++;
            }
        }

        public void SetMood(CustomerMood newMood)
        {
            mood = newMood;
        }
    }
}