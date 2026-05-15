using System.Collections.Generic;
using Project.Input;
using Project.Player;
using Project.Progression;
using Project.SlotMachines;
using Project.SlotMachines.UI;
using UnityEngine;

namespace Project.Managers
{
    public sealed class SlotMachineManager : MonoBehaviour
    {
        [Header("Global Slot Settings")]
        [SerializeField, Min(0.1f)] private float spinDurationSeconds = 0.35f;

        private readonly List<SlotMachine> registeredMachines = new();

        private PlayerBalanceManager playerBalanceManager;
        private CursorManager cursorManager;
        private CasinoProgressionManager casinoProgressionManager;
        private FirstPersonController player;
        private PlayerInteractionController playerInteractionController;
        private PlayerInputHandler inputHandler;

        private SlotMachineCollectionPanelView collectionPanelView;
        private SlotMachine activeCollectionSlotMachine;
        private bool isCollectionPanelOpen;

        public float SpinDurationSeconds => spinDurationSeconds;
        public bool IsCollectionPanelOpen => isCollectionPanelOpen;

        public void Initialize(
            FirstPersonController newPlayer,
            PlayerBalanceManager balanceManager,
            CursorManager newCursorManager,
            CasinoProgressionManager newCasinoProgressionManager)
        {
            player = newPlayer;
            playerBalanceManager = balanceManager;
            cursorManager = newCursorManager;
            casinoProgressionManager = newCasinoProgressionManager;

            if (player == null)
            {
                Debug.LogError($"{nameof(SlotMachineManager)} cannot initialize because player is missing.", this);
                return;
            }

            if (playerBalanceManager == null)
            {
                Debug.LogError($"{nameof(SlotMachineManager)} cannot initialize because PlayerBalanceManager is missing.", this);
                return;
            }

            if (cursorManager == null)
            {
                Debug.LogError($"{nameof(SlotMachineManager)} cannot initialize because CursorManager is missing.", this);
                return;
            }

            if (casinoProgressionManager == null)
            {
                Debug.LogError($"{nameof(SlotMachineManager)} cannot initialize because CasinoProgressionManager is missing.", this);
                return;
            }

            playerInteractionController = player.GetComponent<PlayerInteractionController>();
            inputHandler = player.GetComponent<PlayerInputHandler>();

            if (inputHandler != null)
            {
                inputHandler.CancelPressed += HandleCancelPressed;
            }

            RefreshSceneMachines();
            CreateCollectionPanelIfNeeded();
            HideCollectionPanel();

            Debug.Log($"{nameof(SlotMachineManager)} initialized with {registeredMachines.Count} registered slot machine(s).", this);
        }

        private void OnDestroy()
        {
            if (inputHandler != null)
            {
                inputHandler.CancelPressed -= HandleCancelPressed;
            }

            if (collectionPanelView != null)
            {
                collectionPanelView.CollectRequested -= HandleCollectRequested;
                collectionPanelView.CloseRequested -= HandleCloseRequested;
            }
        }

        public void Register(SlotMachine slotMachine)
        {
            if (slotMachine == null || registeredMachines.Contains(slotMachine))
            {
                return;
            }

            registeredMachines.Add(slotMachine);
        }

        public void Unregister(SlotMachine slotMachine)
        {
            if (slotMachine == null)
            {
                return;
            }

            registeredMachines.Remove(slotMachine);
        }

        public bool TryGetAvailableMachine(out SlotMachine slotMachine)
        {
            slotMachine = null;

            float bestScore = float.NegativeInfinity;

            for (int i = registeredMachines.Count - 1; i >= 0; i--)
            {
                SlotMachine machine = registeredMachines[i];

                if (machine == null)
                {
                    registeredMachines.RemoveAt(i);
                    continue;
                }

                if (!machine.IsAvailable)
                {
                    continue;
                }

                float score = machine.AttractionScore + Random.Range(0f, 0.15f);

                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                slotMachine = machine;
            }

            return slotMachine != null;
        }

        public void SetSpinDuration(float seconds)
        {
            spinDurationSeconds = Mathf.Max(0.1f, seconds);
        }

        public void ShowCollectionPanel(SlotMachine slotMachine)
        {
            if (slotMachine == null)
            {
                return;
            }

            if (slotMachine.StoredCashFromDeposits <= 0)
            {
                Debug.Log($"{slotMachine.name} has no stored cash to collect.", slotMachine);
                return;
            }

            activeCollectionSlotMachine = slotMachine;

            CreateCollectionPanelIfNeeded();

            collectionPanelView.Show(activeCollectionSlotMachine);
            isCollectionPanelOpen = true;

            if (player != null)
            {
                player.SetCanMove(false);
            }

            if (playerInteractionController != null)
            {
                playerInteractionController.SetCanInteract(false);
            }

            if (cursorManager != null)
            {
                cursorManager.EnableMenuCursorMode();
            }
        }

        public void HideCollectionPanel()
        {
            activeCollectionSlotMachine = null;
            isCollectionPanelOpen = false;

            if (collectionPanelView != null)
            {
                collectionPanelView.Hide();
            }

            if (player != null)
            {
                player.SetCanMove(true);
            }

            if (playerInteractionController != null)
            {
                playerInteractionController.SetCanInteract(true);
            }

            if (cursorManager != null)
            {
                cursorManager.EnableGameplayCursorMode();
            }
        }

        public void AwardSlotMachineXp(CasinoXpSource source, SlotMachine slotMachine)
        {
            if (casinoProgressionManager == null || slotMachine == null)
            {
                return;
            }

            float multiplier = GetSlotMachineXpMultiplier(slotMachine);
            casinoProgressionManager.AddConfiguredXp(source, multiplier);
        }

        private void CreateCollectionPanelIfNeeded()
        {
            if (collectionPanelView != null)
            {
                return;
            }

            collectionPanelView = SlotMachineCollectionPanelView.Create();
            collectionPanelView.CollectRequested += HandleCollectRequested;
            collectionPanelView.CloseRequested += HandleCloseRequested;
        }

        private void HandleCollectRequested(int requestedAmount)
        {
            if (activeCollectionSlotMachine == null)
            {
                HideCollectionPanel();
                return;
            }

            if (requestedAmount <= 0)
            {
                return;
            }

            bool collected = activeCollectionSlotMachine.TryCollectStoredCash(
                requestedAmount,
                out int collectedAmount);

            if (!collected || collectedAmount <= 0)
            {
                return;
            }

            playerBalanceManager.AddBalance(collectedAmount);

            AwardSlotCashCollectedXp(activeCollectionSlotMachine, collectedAmount);

            Debug.Log(
                $"Collected £{collectedAmount} from {activeCollectionSlotMachine.name} into player balance.",
                activeCollectionSlotMachine);

            HideCollectionPanel();
        }

        private void AwardSlotCashCollectedXp(SlotMachine slotMachine, int collectedAmount)
        {
            if (casinoProgressionManager == null || slotMachine == null || collectedAmount <= 0)
            {
                return;
            }

            float multiplier = GetSlotMachineXpMultiplier(slotMachine);

            casinoProgressionManager.AddConfiguredXp(
                CasinoXpSource.SlotCashCollected,
                collectedAmount,
                multiplier);
        }

        private float GetSlotMachineXpMultiplier(SlotMachine slotMachine)
        {
            if (slotMachine == null || slotMachine.Config == null)
            {
                return 1f;
            }

            return Mathf.Max(0f, slotMachine.Config.CasinoXpMultiplier);
        }

        public void RefreshSceneMachines()
        {
            registeredMachines.RemoveAll(machine => machine == null);

            SlotMachine[] sceneMachines = FindObjectsByType<SlotMachine>(FindObjectsSortMode.None);

            foreach (SlotMachine machine in sceneMachines)
            {
                Register(machine);
            }
        }

        private void HandleCloseRequested()
        {
            HideCollectionPanel();
        }

        private void HandleCancelPressed()
        {
            if (!isCollectionPanelOpen)
            {
                return;
            }

            HideCollectionPanel();
        }
    }
}