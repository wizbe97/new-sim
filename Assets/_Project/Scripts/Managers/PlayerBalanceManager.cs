using System;
using Project.Economy;
using Project.Events;
using UnityEngine;

namespace Project.Managers
{
    public class PlayerBalanceManager : MonoBehaviour
    {
        [Header("Balance Data")]
        [SerializeField] private PlayerBalanceSO playerBalance;

        [Header("Events")]
        [SerializeField] private GameEventSO balanceChangedEvent;

        public event Action<int> BalanceChanged;

        public int CurrentBalance
        {
            get
            {
                if (playerBalance == null)
                {
                    Debug.LogWarning("PlayerBalanceManager has no PlayerBalanceSO assigned.");
                    return 0;
                }

                return playerBalance.CurrentBalance;
            }
        }

        public void Initialize()
        {
            if (playerBalance == null)
            {
                Debug.LogError("PlayerBalanceManager cannot initialize because PlayerBalanceSO is missing.");
                return;
            }

            playerBalance.ResetBalance();
            NotifyBalanceChanged();

            Debug.Log($"PlayerBalanceManager initialized with balance: {playerBalance.CurrentBalance}");
        }

        public bool CanAfford(int amount)
        {
            if (playerBalance == null)
            {
                Debug.LogWarning("Cannot check affordability because PlayerBalanceSO is missing.");
                return false;
            }

            return playerBalance.CanAfford(amount);
        }

        public bool DeductBalance(int amount)
        {
            if (playerBalance == null)
            {
                Debug.LogWarning("Cannot deduct balance because PlayerBalanceSO is missing.");
                return false;
            }

            bool deducted = playerBalance.DeductBalance(amount);

            if (!deducted)
            {
                Debug.Log($"Could not deduct {amount}. Current balance: {playerBalance.CurrentBalance}");
                return false;
            }

            NotifyBalanceChanged();

            Debug.Log($"Deducted {amount}. New balance: {playerBalance.CurrentBalance}");
            return true;
        }

        public void AddBalance(int amount)
        {
            if (playerBalance == null)
            {
                Debug.LogWarning("Cannot add balance because PlayerBalanceSO is missing.");
                return;
            }

            playerBalance.AddBalance(amount);
            NotifyBalanceChanged();

            Debug.Log($"Added {amount}. New balance: {playerBalance.CurrentBalance}");
        }

        public void ResetBalance()
        {
            if (playerBalance == null)
            {
                Debug.LogWarning("Cannot reset balance because PlayerBalanceSO is missing.");
                return;
            }

            playerBalance.ResetBalance();
            NotifyBalanceChanged();

            Debug.Log($"Balance reset to: {playerBalance.CurrentBalance}");
        }

        private void NotifyBalanceChanged()
        {
            int newBalance = playerBalance != null ? playerBalance.CurrentBalance : 0;

            BalanceChanged?.Invoke(newBalance);

            if (balanceChangedEvent != null)
            {
                balanceChangedEvent.Raise();
            }
        }
    }
}