using System;
using System.Collections.Generic;
using Project.Interfaces;
using Project.Managers;
using Project.NPC.Customer;
using Project.Progression;
using UnityEngine;

namespace Project.SlotMachines
{
    public sealed class SlotMachine : MonoBehaviour, ISecondaryInteractable
    {
        [Header("Config")]
        [SerializeField] private SlotMachineConfigSO config;

        [Header("Play Point")]
        [Tooltip("Where NPCs stand to play this slot machine.")]
        [SerializeField] private Transform playPoint;

        [Header("Staff Collection Points")]
        [Tooltip("Ordered positions staff can stand at to collect cash. Index 0 is tried first, then index 1, and so on.")]
        [SerializeField] private List<Transform> staffCollectionPoints = new();

        [Header("Runtime Financials")]
        [SerializeField] private int storedCashFromDeposits;
        [SerializeField] private int currentSessionCredit;
        [SerializeField] private int lifetimeDeposited;
        [SerializeField] private int lifetimeTicketValuePrinted;

        [Header("Runtime State")]
        [SerializeField] private bool isBeingMovedForPlacement;

        private SlotMachineManager manager;
        private CustomerController reservedBy;
        private CustomerController activeCustomer;

        public event Action FinancialsChanged;

        public SlotMachineConfigSO Config => config;
        public Transform PlayPoint => playPoint;
        public IReadOnlyList<Transform> StaffCollectionPoints => staffCollectionPoints;

        public bool IsReserved => reservedBy != null;
        public bool IsOccupied => activeCustomer != null;

        public bool IsAvailable =>
            config != null &&
            playPoint != null &&
            !isBeingMovedForPlacement &&
            !IsReserved &&
            !IsOccupied;

        public float AttractionScore => config != null ? config.AttractionScore : 0f;

        public int MachineCredit => currentSessionCredit;
        public int CurrentSessionCredit => currentSessionCredit;
        public int StoredCashFromDeposits => storedCashFromDeposits;
        public int LifetimeDeposited => lifetimeDeposited;
        public int LifetimeTicketValuePrinted => lifetimeTicketValuePrinted;
        public bool IsBeingMovedForPlacement => isBeingMovedForPlacement;

        public string SecondaryInteractionPrompt => $"Collect slot cash: £{storedCashFromDeposits}";
        public bool CanSecondaryInteract => storedCashFromDeposits > 0 && !isBeingMovedForPlacement;

        private void Awake()
        {
            if (playPoint == null)
            {
                CreateDefaultPlayPoint();
            }
        }

        private void OnEnable()
        {
            TryRegisterWithManager();
            NotifyFinancialsChanged();
        }

        private void Start()
        {
            TryRegisterWithManager();
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.Unregister(this);
            }
        }

        public bool TryGetStaffCollectionPoint(int index, out Transform collectionPoint)
        {
            collectionPoint = null;

            if (index < 0 || index >= staffCollectionPoints.Count)
            {
                return false;
            }

            collectionPoint = staffCollectionPoints[index];
            return collectionPoint != null;
        }

        public bool HasAnyStaffCollectionPoint()
        {
            for (int i = 0; i < staffCollectionPoints.Count; i++)
            {
                if (staffCollectionPoints[i] != null)
                {
                    return true;
                }
            }

            return false;
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

            if (isBeingMovedForPlacement)
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

            storedCashFromDeposits += safeDepositAmount;
            lifetimeDeposited += safeDepositAmount;

            SetCurrentSessionCredit(safeDepositAmount);

            Debug.Log(
                $"{customer.name} deposited £{safeDepositAmount} into {name}. Stored Cash: £{storedCashFromDeposits}, Current Credit: £{currentSessionCredit}.",
                this);

            return currentSessionCredit > 0;
        }

        public SlotSpinResult Spin()
        {
            if (isBeingMovedForPlacement || config == null || currentSessionCredit <= 0)
            {
                return SlotSpinResult.Empty;
            }

            int betAmount = Mathf.Clamp(currentSessionCredit, 1, config.MaximumBet);

            int creditAfterBet = currentSessionCredit - betAmount;
            int payoutAmount = config.GeneratePayout(betAmount);
            int creditAfterSpin = creditAfterBet + payoutAmount;

            SetCurrentSessionCredit(creditAfterSpin);

            AwardSlotMachineXp(CasinoXpSource.SlotSpin);

            return new SlotSpinResult(betAmount, payoutAmount, currentSessionCredit);
        }

        public int PrintTicketAndEndSession(CustomerController customer)
        {
            if (customer == null || activeCustomer != customer)
            {
                return 0;
            }

            int ticketAmount = currentSessionCredit;

            lifetimeTicketValuePrinted += ticketAmount;

            Debug.Log(
                $"{customer.name} printed a ticket from {name} for £{ticketAmount}. Stored Cash: £{storedCashFromDeposits}, Current Credit: £0.",
                this);

            SetCurrentSessionCredit(0);

            activeCustomer = null;
            reservedBy = null;

            AwardSlotMachineXp(CasinoXpSource.SlotSessionCompleted);

            return ticketAmount;
        }

        public int EndSessionWithoutTicket(CustomerController customer)
        {
            if (customer == null || activeCustomer != customer)
            {
                return 0;
            }

            int remainingCredit = currentSessionCredit;

            Debug.Log(
                $"{customer.name} ended session on {name} without printing a ticket. Remaining Credit: £{remainingCredit}.",
                this);

            SetCurrentSessionCredit(0);

            activeCustomer = null;
            reservedBy = null;

            AwardSlotMachineXp(CasinoXpSource.SlotSessionCompleted);

            return remainingCredit;
        }

        public void ReleaseReservation(CustomerController customer)
        {
            if (reservedBy == customer)
            {
                reservedBy = null;
            }
        }

        public bool TryCollectStoredCash(int requestedAmount, out int collectedAmount)
        {
            collectedAmount = 0;

            if (requestedAmount <= 0 || storedCashFromDeposits <= 0)
            {
                return false;
            }

            collectedAmount = Mathf.Clamp(requestedAmount, 0, storedCashFromDeposits);
            storedCashFromDeposits -= collectedAmount;

            NotifyFinancialsChanged();

            Debug.Log(
                $"Collected £{collectedAmount} from {name}. Remaining stored cash: £{storedCashFromDeposits}.",
                this);

            return collectedAmount > 0;
        }

        public void SecondaryInteract()
        {
            if (!CanSecondaryInteract)
            {
                return;
            }

            TryRegisterWithManager();

            if (manager == null)
            {
                Debug.LogWarning($"{name} cannot open collection UI because no {nameof(SlotMachineManager)} exists.", this);
                return;
            }

            manager.ShowCollectionPanel(this);
        }

        public void HandlePickedUpForPlacement()
        {
            isBeingMovedForPlacement = true;

            CustomerController customerToNotify = activeCustomer != null
                ? activeCustomer
                : reservedBy;

            int forcedTicketAmount = currentSessionCredit;

            if (forcedTicketAmount > 0)
            {
                lifetimeTicketValuePrinted += forcedTicketAmount;
            }

            SetCurrentSessionCredit(0);

            activeCustomer = null;
            reservedBy = null;

            if (customerToNotify != null)
            {
                customerToNotify.NotifySlotMachineBecameUnavailable(this, forcedTicketAmount);
            }

            Debug.Log(
                $"{name} was picked up. Active slot gameplay was interrupted. Forced ticket amount: £{forcedTicketAmount}. Stored Cash remains: £{storedCashFromDeposits}.",
                this);
        }

        public void HandlePlacedAfterPlacement()
        {
            isBeingMovedForPlacement = false;
            TryRegisterWithManager();
            NotifyFinancialsChanged();

            Debug.Log($"{name} was placed and is now discoverable again.", this);
        }

        private void AwardSlotMachineXp(CasinoXpSource source)
        {
            TryRegisterWithManager();

            if (manager == null)
            {
                return;
            }

            manager.AwardSlotMachineXp(source, this);
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

        private void TryRegisterWithManager()
        {
            if (manager != null)
            {
                return;
            }

            manager = FindFirstObjectByType<SlotMachineManager>();

            if (manager == null)
            {
                return;
            }

            manager.Register(this);
        }

        private void CreateDefaultPlayPoint()
        {
            GameObject playPointObject = new GameObject("PlayPoint");
            playPoint = playPointObject.transform;
            playPoint.SetParent(transform, false);
            playPoint.localPosition = new Vector3(0f, 0f, -1.25f);
            playPoint.localRotation = Quaternion.identity;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = staffCollectionPoints.Count - 1; i >= 0; i--)
            {
                if (staffCollectionPoints[i] == null)
                {
                    staffCollectionPoints.RemoveAt(i);
                }
            }
        }
#endif
    }
}