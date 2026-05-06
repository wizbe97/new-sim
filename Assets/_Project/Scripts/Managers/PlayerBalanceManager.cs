using System;
using Project.Economy;
using UnityEngine;

namespace Project.Managers
{
    public sealed class PlayerBalanceManager : MonoBehaviour
    {
        [Header("Balance Config")]
        [SerializeField] private PlayerBalanceSO playerBalance;

        private int currentBalance;

        public event Action<int> BalanceChanged;

        public int CurrentBalance => currentBalance;

        public void Initialize()
        {
            if (playerBalance == null)
            {
                Debug.LogError($"{nameof(PlayerBalanceManager)} cannot initialize because PlayerBalanceSO is missing.", this);
                currentBalance = 0;
                NotifyBalanceChanged();
                return;
            }

            currentBalance = Mathf.Max(0, playerBalance.StartingBalance);
            NotifyBalanceChanged();

            Debug.Log($"{nameof(PlayerBalanceManager)} initialized with balance: {currentBalance}", this);
        }

        public bool CanAfford(int amount)
        {
            if (!IsValidAmount(amount, nameof(CanAfford)))
            {
                return false;
            }

            return currentBalance >= amount;
        }

        public bool TrySpend(int amount)
        {
            if (!IsValidAmount(amount, nameof(TrySpend)))
            {
                return false;
            }

            if (!CanAfford(amount))
            {
                Debug.Log($"Could not spend {amount}. Current balance: {currentBalance}", this);
                return false;
            }

            currentBalance -= amount;
            NotifyBalanceChanged();

            Debug.Log($"Spent {amount}. New balance: {currentBalance}", this);
            return true;
        }

        public void AddBalance(int amount)
        {
            if (!IsValidAmount(amount, nameof(AddBalance)))
            {
                return;
            }

            currentBalance += amount;
            NotifyBalanceChanged();

            Debug.Log($"Added {amount}. New balance: {currentBalance}", this);
        }

        public void SetBalance(int amount)
        {
            currentBalance = Mathf.Max(0, amount);
            NotifyBalanceChanged();
        }

        public void ResetBalance()
        {
            currentBalance = playerBalance != null
                ? Mathf.Max(0, playerBalance.StartingBalance)
                : 0;

            NotifyBalanceChanged();

            Debug.Log($"Balance reset to: {currentBalance}", this);
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

        private void NotifyBalanceChanged()
        {
            BalanceChanged?.Invoke(currentBalance);
        }
    }
}