using UnityEngine;

namespace Project.Economy
{
    [CreateAssetMenu(
        fileName = "PlayerBalance",
        menuName = "Project/Economy/Player Balance")]
    public class PlayerBalanceSO : ScriptableObject
    {
        [Header("Starting Values")]
        [SerializeField] private int startingBalance = 500;

        [Header("Runtime Values")]
        [SerializeField] private int currentBalance;

        public int StartingBalance => startingBalance;
        public int CurrentBalance => currentBalance;

        public void ResetBalance()
        {
            currentBalance = Mathf.Max(0, startingBalance);
        }

        public bool CanAfford(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning("CanAfford was called with a negative amount.");
                return false;
            }

            return currentBalance >= amount;
        }

        public bool DeductBalance(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning("DeductBalance was called with a negative amount.");
                return false;
            }

            if (!CanAfford(amount))
            {
                return false;
            }

            currentBalance -= amount;
            return true;
        }

        public void AddBalance(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning("AddBalance was called with a negative amount.");
                return;
            }

            currentBalance += amount;
        }

        public void SetBalance(int amount)
        {
            currentBalance = Mathf.Max(0, amount);
        }
    }
}