using System;
using Project.NPC.Customer;
using UnityEngine;

namespace Project.SlotMachines
{
    public sealed class SlotMachine : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private SlotMachineConfigSO config;

        [Header("Play Point")]
        [Tooltip("Where NPCs stand to play this slot machine.")]
        [SerializeField] private Transform playPoint;

        [Header("Runtime Financials")]
        [SerializeField] private int currentSessionCredit;
        [SerializeField] private int currentSessionStartingDeposit;
        [SerializeField] private int realisedProfitLoss;
        [SerializeField] private int lifetimeDeposited;
        [SerializeField] private int lifetimeWithdrawn;

        private SlotMachineManager manager;
        private CustomerController reservedBy;
        private CustomerController activeCustomer;

        public event Action FinancialsChanged;

        public SlotMachineConfigSO Config => config;
        public Transform PlayPoint => playPoint;

        public bool IsReserved => reservedBy != null;
        public bool IsOccupied => activeCustomer != null;
        public bool IsAvailable => config != null && playPoint != null && !IsReserved && !IsOccupied;

        public float AttractionScore => config != null ? config.AttractionScore : 0f;

        public int MachineCredit => currentSessionCredit;
        public int CurrentSessionCredit => currentSessionCredit;
        public int CurrentSessionStartingDeposit => currentSessionStartingDeposit;
        public int RealisedProfitLoss => realisedProfitLoss;
        public int LifetimeDeposited => lifetimeDeposited;
        public int LifetimeWithdrawn => lifetimeWithdrawn;

        public int CurrentSessionProfitLoss => currentSessionStartingDeposit - currentSessionCredit;
        public int DisplayedTotalProfitLoss => realisedProfitLoss + CurrentSessionProfitLoss;

        private void Awake()
        {
            if (playPoint == null)
            {
                CreateDefaultPlayPoint();
            }
        }

        private void OnEnable()
        {
            manager = FindFirstObjectByType<SlotMachineManager>();

            if (manager == null)
            {
                GameObject managerObject = new GameObject(nameof(SlotMachineManager));
                manager = managerObject.AddComponent<SlotMachineManager>();
            }

            manager.Register(this);
            NotifyFinancialsChanged();
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.Unregister(this);
            }
        }

        public bool TryReserve(CustomerController customer)
        {
            if (customer == null || !IsAvailable)
            {
                return false;
            }

            reservedBy = customer;
            return true;
        }

        public bool TryBeginSession(CustomerController customer, int depositAmount)
        {
            if (customer == null)
            {
                return false;
            }

            if (reservedBy != customer)
            {
                return false;
            }

            if (activeCustomer != null)
            {
                return false;
            }

            int safeDepositAmount = Mathf.Max(0, depositAmount);

            activeCustomer = customer;
            currentSessionStartingDeposit = safeDepositAmount;
            lifetimeDeposited += safeDepositAmount;

            SetCurrentSessionCredit(safeDepositAmount);

            Debug.Log(
                $"{customer.name} deposited £{safeDepositAmount} into {name}. Machine P/L: {FormatSignedCurrency(DisplayedTotalProfitLoss)}, Session Credit: £{currentSessionCredit}.",
                this);

            return currentSessionCredit > 0;
        }

        public SlotSpinResult Spin()
        {
            if (config == null || currentSessionCredit <= 0)
            {
                return SlotSpinResult.Empty;
            }

            int betAmount = Mathf.Clamp(currentSessionCredit, 1, config.MaximumBet);

            int creditAfterBet = currentSessionCredit - betAmount;
            int payoutAmount = config.GeneratePayout(betAmount);
            int creditAfterSpin = creditAfterBet + payoutAmount;

            SetCurrentSessionCredit(creditAfterSpin);

            return new SlotSpinResult(betAmount, payoutAmount, currentSessionCredit);
        }

        public int EndSession(CustomerController customer)
        {
            if (customer == null || activeCustomer != customer)
            {
                return 0;
            }

            int withdrawnAmount = currentSessionCredit;
            int sessionProfitLoss = currentSessionStartingDeposit - withdrawnAmount;

            realisedProfitLoss += sessionProfitLoss;
            lifetimeWithdrawn += withdrawnAmount;

            Debug.Log(
                $"{customer.name} stopped playing {name} and withdrew £{withdrawnAmount}. Session P/L: {FormatSignedCurrency(sessionProfitLoss)}, Total Machine P/L: {FormatSignedCurrency(realisedProfitLoss)}.",
                this);

            currentSessionStartingDeposit = 0;
            SetCurrentSessionCredit(0);

            activeCustomer = null;
            reservedBy = null;

            return withdrawnAmount;
        }

        public void ReleaseReservation(CustomerController customer)
        {
            if (reservedBy == customer)
            {
                reservedBy = null;
            }
        }

        private void SetCurrentSessionCredit(int newCredit)
        {
            currentSessionCredit = Mathf.Max(0, newCredit);
            NotifyFinancialsChanged();
        }

        private void NotifyFinancialsChanged()
        {
            FinancialsChanged?.Invoke();
        }

        private void CreateDefaultPlayPoint()
        {
            GameObject playPointObject = new GameObject("PlayPoint");
            playPoint = playPointObject.transform;
            playPoint.SetParent(transform, false);
            playPoint.localPosition = new Vector3(0f, 0f, -1.25f);
            playPoint.localRotation = Quaternion.identity;
        }

        private static string FormatSignedCurrency(int amount)
        {
            return amount >= 0
                ? $"£{amount}"
                : $"-£{Mathf.Abs(amount)}";
        }
    }
}