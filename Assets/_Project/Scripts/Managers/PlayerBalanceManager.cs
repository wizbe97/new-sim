using System;
using Project.Economy;
using UnityEngine;

namespace Project.Managers
{
    public sealed class PlayerBalanceManager : MonoBehaviour
    {
        [Header("Balance Config")]
        [SerializeField] private PlayerBalanceSO playerBalance;

        private CasinoProgressionManager casinoProgressionManager;

        private int currentCasinoFunds;
        private int currentReserveFund;
        private int currentPlayerBalance;
        private int minimumReserveFund;

        public event Action<int> BalanceChanged;
        public event Action FundsChanged;

        public int CurrentBalance => currentCasinoFunds;

        public int CurrentCasinoFunds => currentCasinoFunds;
        public int CurrentReserveFund => currentReserveFund;
        public int CurrentPlayerBalance => currentPlayerBalance;
        public int MinimumReserveFund => minimumReserveFund;

        public bool IsReserveBelowMinimum => currentReserveFund < minimumReserveFund;

        public void Initialize(CasinoProgressionManager progressionManager)
        {
            if (progressionManager == null)
            {
                Debug.LogError($"{nameof(PlayerBalanceManager)} cannot initialize because CasinoProgressionManager is missing.", this);
                casinoProgressionManager = null;
            }
            else
            {
                casinoProgressionManager = progressionManager;
                casinoProgressionManager.LevelChanged += HandleCasinoLevelChanged;
            }

            if (playerBalance == null)
            {
                Debug.LogError($"{nameof(PlayerBalanceManager)} cannot initialize because PlayerBalanceSO is missing.", this);

                currentCasinoFunds = 0;
                currentReserveFund = 0;
                currentPlayerBalance = 0;
                minimumReserveFund = 0;

                NotifyFundsChanged();
                return;
            }

            currentCasinoFunds = Mathf.Max(0, playerBalance.StartingCasinoFunds);
            currentReserveFund = Mathf.Max(0, playerBalance.StartingReserveFund);
            currentPlayerBalance = Mathf.Max(0, playerBalance.StartingPlayerBalance);

            RecalculateMinimumReserveFund();

            NotifyFundsChanged();

            Debug.Log(
                $"{nameof(PlayerBalanceManager)} initialized. " +
                $"Casino Funds: {currentCasinoFunds}, Reserve Fund: {currentReserveFund}, Player Balance: {currentPlayerBalance}, Minimum Reserve: {minimumReserveFund}",
                this);
        }

        private void OnDestroy()
        {
            if (casinoProgressionManager != null)
            {
                casinoProgressionManager.LevelChanged -= HandleCasinoLevelChanged;
            }
        }

        public bool CanAfford(int amount)
        {
            return CanAffordCasinoFunds(amount);
        }

        public bool TrySpend(int amount)
        {
            return TrySpendCasinoFunds(amount);
        }

        public void AddBalance(int amount)
        {
            AddCasinoIncome(amount);
        }

        public bool CanAffordCasinoFunds(int amount)
        {
            if (!IsValidAmount(amount, nameof(CanAffordCasinoFunds)))
            {
                return false;
            }

            return currentCasinoFunds >= amount;
        }

        public bool TrySpendCasinoFunds(int amount)
        {
            if (!IsValidAmount(amount, nameof(TrySpendCasinoFunds)))
            {
                return false;
            }

            if (!CanAffordCasinoFunds(amount))
            {
                Debug.Log($"Could not spend {amount} from Casino Funds. Current Casino Funds: {currentCasinoFunds}", this);
                return false;
            }

            currentCasinoFunds -= amount;
            NotifyFundsChanged();

            Debug.Log($"Spent {amount} from Casino Funds. New Casino Funds: {currentCasinoFunds}", this);
            return true;
        }

        public bool CanAffordCashOut(int amount)
        {
            if (!IsValidAmount(amount, nameof(CanAffordCashOut)))
            {
                return false;
            }

            return currentCasinoFunds + currentReserveFund >= amount;
        }

        public bool TryPayCashOut(int amount)
        {
            if (!IsValidAmount(amount, nameof(TryPayCashOut)))
            {
                return false;
            }

            if (!CanAffordCashOut(amount))
            {
                Debug.Log(
                    $"Could not pay cash-out ticket of {amount}. " +
                    $"Casino Funds: {currentCasinoFunds}, Reserve Fund: {currentReserveFund}",
                    this);

                return false;
            }

            int remainingAmount = amount;

            int casinoFundsContribution = Mathf.Min(currentCasinoFunds, remainingAmount);
            currentCasinoFunds -= casinoFundsContribution;
            remainingAmount -= casinoFundsContribution;

            int reserveContribution = 0;

            if (remainingAmount > 0)
            {
                reserveContribution = Mathf.Min(currentReserveFund, remainingAmount);
                currentReserveFund -= reserveContribution;
                remainingAmount -= reserveContribution;
            }

            NotifyFundsChanged();

            Debug.Log(
                $"Paid cash-out ticket of {amount}. " +
                $"Casino Funds used: {casinoFundsContribution}, Reserve Fund used: {reserveContribution}. " +
                $"New Casino Funds: {currentCasinoFunds}, New Reserve Fund: {currentReserveFund}",
                this);

            return true;
        }

        public void AddCasinoIncome(int amount)
        {
            if (!IsValidAmount(amount, nameof(AddCasinoIncome)))
            {
                return;
            }

            if (amount <= 0)
            {
                return;
            }

            int reserveTopUpAmount = CalculateReserveTopUpAmount(amount);
            int casinoFundsAmount = amount - reserveTopUpAmount;

            currentReserveFund += reserveTopUpAmount;
            currentCasinoFunds += casinoFundsAmount;

            NotifyFundsChanged();

            Debug.Log(
                $"Added casino income {amount}. Casino Funds +{casinoFundsAmount}, Reserve Fund +{reserveTopUpAmount}. " +
                $"New Casino Funds: {currentCasinoFunds}, New Reserve Fund: {currentReserveFund}, Minimum Reserve: {minimumReserveFund}",
                this);
        }

        public void AddCasinoFundsDirect(int amount)
        {
            if (!IsValidAmount(amount, nameof(AddCasinoFundsDirect)))
            {
                return;
            }

            currentCasinoFunds += amount;
            NotifyFundsChanged();

            Debug.Log($"Added {amount} directly to Casino Funds. New Casino Funds: {currentCasinoFunds}", this);
        }

        public void AddReserveFundDirect(int amount)
        {
            if (!IsValidAmount(amount, nameof(AddReserveFundDirect)))
            {
                return;
            }

            currentReserveFund += amount;
            NotifyFundsChanged();

            Debug.Log($"Added {amount} directly to Reserve Fund. New Reserve Fund: {currentReserveFund}", this);
        }

        public void AddPlayerBalance(int amount)
        {
            if (!IsValidAmount(amount, nameof(AddPlayerBalance)))
            {
                return;
            }

            currentPlayerBalance += amount;
            NotifyFundsChanged();

            Debug.Log($"Added {amount} to Player Balance. New Player Balance: {currentPlayerBalance}", this);
        }

        public void SetBalance(int amount)
        {
            SetCasinoFunds(amount);
        }

        public void SetCasinoFunds(int amount)
        {
            currentCasinoFunds = Mathf.Max(0, amount);
            NotifyFundsChanged();
        }

        public void SetReserveFund(int amount)
        {
            currentReserveFund = Mathf.Max(0, amount);
            NotifyFundsChanged();
        }

        public void SetPlayerBalance(int amount)
        {
            currentPlayerBalance = Mathf.Max(0, amount);
            NotifyFundsChanged();
        }

        public void ResetBalance()
        {
            currentCasinoFunds = playerBalance != null
                ? Mathf.Max(0, playerBalance.StartingCasinoFunds)
                : 0;

            currentReserveFund = playerBalance != null
                ? Mathf.Max(0, playerBalance.StartingReserveFund)
                : 0;

            currentPlayerBalance = playerBalance != null
                ? Mathf.Max(0, playerBalance.StartingPlayerBalance)
                : 0;

            RecalculateMinimumReserveFund();
            NotifyFundsChanged();

            Debug.Log(
                $"Funds reset. Casino Funds: {currentCasinoFunds}, Reserve Fund: {currentReserveFund}, Player Balance: {currentPlayerBalance}",
                this);
        }

        private int CalculateReserveTopUpAmount(int incomeAmount)
        {
            if (playerBalance == null)
            {
                return 0;
            }

            if (!IsReserveBelowMinimum)
            {
                return 0;
            }

            int reserveShortfall = minimumReserveFund - currentReserveFund;

            if (reserveShortfall <= 0)
            {
                return 0;
            }

            int calculatedTopUp = Mathf.CeilToInt(incomeAmount * playerBalance.ReserveTopUpPercentageWhenBelowMinimum);
            calculatedTopUp = Mathf.Clamp(calculatedTopUp, 0, incomeAmount);

            return Mathf.Min(calculatedTopUp, reserveShortfall);
        }

        private void HandleCasinoLevelChanged(int newLevel)
        {
            RecalculateMinimumReserveFund();
            NotifyFundsChanged();
        }

        private void RecalculateMinimumReserveFund()
        {
            if (playerBalance == null)
            {
                minimumReserveFund = 0;
                return;
            }

            int casinoLevel = casinoProgressionManager != null
                ? Mathf.Max(1, casinoProgressionManager.CurrentLevel)
                : 1;

            minimumReserveFund = Mathf.Max(0, playerBalance.BaseMinimumReserveFundPerCasinoLevel * casinoLevel);
        }

        private bool IsValidAmount(int amount, string methodName)
        {
            if (amount >= 0)
            {
                return true;
            }

            Debug.LogWarning($"{nameof(PlayerBalanceManager)}.{methodName} was called with a negative amount.", this);
            return false;
        }

        private void NotifyFundsChanged()
        {
            BalanceChanged?.Invoke(currentCasinoFunds);
            FundsChanged?.Invoke();
        }
    }
}