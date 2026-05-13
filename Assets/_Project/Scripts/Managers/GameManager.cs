using System;
using Project.Hands;
using Project.Placement;
using Project.Shop;
using Project.Spawning;
using UnityEngine;

namespace Project.Managers
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Manager Prefabs")]
        [SerializeField] private UIManager uiManagerPrefab;
        [SerializeField] private CursorManager cursorManagerPrefab;
        [SerializeField] private BuildingManager buildingManagerPrefab;
        [SerializeField] private PlayerBalanceManager playerBalanceManagerPrefab;
        [SerializeField] private PhoneManager phoneManagerPrefab;
        [SerializeField] private PlayerManager playerManagerPrefab;
        [SerializeField] private CameraManager cameraManagerPrefab;
        [SerializeField] private ItemDeliveryManager itemDeliveryManagerPrefab;
        [SerializeField] private InHandManager inHandManagerPrefab;
        [SerializeField] private ShopPurchaseService shopPurchaseServicePrefab;
        [SerializeField] private ShopUIController shopUIControllerPrefab;
        [SerializeField] private SlotMachineManager slotMachineManagerPrefab;
        [SerializeField] private CashDeskManager cashDeskManagerPrefab;

        [Header("Scene References")]
        [SerializeField] private PlayerSpawnPoint playerSpawnPoint;
        [SerializeField] private Transform itemDeliveryPad;

        [Header("Cash Desk Scene References")]
        [SerializeField] private Transform cashDeskQueueStartPoint;
        [SerializeField] private Transform cashDeskTicketPlacementPoint;

        private UIManager uiManager;
        private CursorManager cursorManager;
        private BuildingManager buildingManager;
        private PlayerBalanceManager playerBalanceManager;
        private PhoneManager phoneManager;
        private PlayerManager playerManager;
        private CameraManager cameraManager;
        private ItemDeliveryManager itemDeliveryManager;
        private InHandManager inHandManager;
        private ShopPurchaseService shopPurchaseService;
        private ShopUIController shopUIController;
        private SlotMachineManager slotMachineManager;
        private CashDeskManager cashDeskManager;

        public UIManager UIManager => uiManager;
        public CursorManager CursorManager => cursorManager;
        public BuildingManager BuildingManager => buildingManager;
        public PlayerBalanceManager PlayerBalanceManager => playerBalanceManager;
        public PhoneManager PhoneManager => phoneManager;
        public PlayerManager PlayerManager => playerManager;
        public CameraManager CameraManager => cameraManager;
        public ItemDeliveryManager ItemDeliveryManager => itemDeliveryManager;
        public InHandManager InHandManager => inHandManager;
        public ShopPurchaseService ShopPurchaseService => shopPurchaseService;
        public ShopUIController ShopUIController => shopUIController;
        public SlotMachineManager SlotMachineManager => slotMachineManager;
        public CashDeskManager CashDeskManager => cashDeskManager;

        private void Awake()
        {
            SpawnManagers();

            if (!HasRequiredManagers())
            {
                Debug.LogError($"{nameof(GameManager)} failed to initialize because one or more required managers are missing.", this);
                return;
            }

            if (!HasRequiredSceneReferences())
            {
                Debug.LogError($"{nameof(GameManager)} failed to initialize because one or more required scene references are missing.", this);
                return;
            }

            InitializeManagers();
        }

        private void SpawnManagers()
        {
            uiManager = SpawnManager(uiManagerPrefab, nameof(UIManager));
            cursorManager = SpawnManager(cursorManagerPrefab, nameof(CursorManager));
            buildingManager = SpawnManager(buildingManagerPrefab, nameof(BuildingManager));
            playerBalanceManager = SpawnManager(playerBalanceManagerPrefab, nameof(PlayerBalanceManager));
            phoneManager = SpawnManager(phoneManagerPrefab, nameof(PhoneManager));
            playerManager = SpawnManager(playerManagerPrefab, nameof(PlayerManager));
            cameraManager = SpawnManager(cameraManagerPrefab, nameof(CameraManager));
            itemDeliveryManager = SpawnManager(itemDeliveryManagerPrefab, nameof(ItemDeliveryManager));
            inHandManager = SpawnManager(inHandManagerPrefab, nameof(InHandManager));
            shopPurchaseService = SpawnManager(shopPurchaseServicePrefab, nameof(ShopPurchaseService));
            shopUIController = SpawnManager(shopUIControllerPrefab, nameof(ShopUIController));
            slotMachineManager = SpawnManager(slotMachineManagerPrefab, nameof(SlotMachineManager));
            cashDeskManager = SpawnManager(cashDeskManagerPrefab, nameof(CashDeskManager));
        }

        private void InitializeManagers()
        {
            Action[] initializationSteps =
            {
                () => playerBalanceManager.Initialize(),
                () => itemDeliveryManager.Initialize(itemDeliveryPad),
                () => shopPurchaseService.Initialize(playerBalanceManager, itemDeliveryManager),
                () => uiManager.Initialize(playerBalanceManager),
                () => shopUIController.Initialize(uiManager, shopPurchaseService),

                () =>
                {
                    playerManager.SetFallbackSpawnPoint(playerSpawnPoint);
                    playerManager.Initialize(uiManager, buildingManager);
                },

                () => cameraManager.Initialize(playerManager.CurrentPlayer),
                () => buildingManager.Initialize(playerManager.CurrentPlayer),
                () => cursorManager.Initialize(uiManager),
                () => phoneManager.Initialize(playerManager.CurrentPlayer, uiManager, cursorManager),
                () => inHandManager.Initialize(playerManager.CurrentPlayer, buildingManager),

                () => slotMachineManager.Initialize(
                    playerManager.CurrentPlayer,
                    playerBalanceManager,
                    cursorManager),

                () => cashDeskManager.Initialize(
                    playerBalanceManager,
                    cashDeskQueueStartPoint,
                    cashDeskTicketPlacementPoint)
            };

            foreach (Action initializeStep in initializationSteps)
            {
                initializeStep.Invoke();
            }
        }

        private bool HasRequiredManagers()
        {
            bool hasRequiredManagers = true;

            hasRequiredManagers &= ValidateManager(uiManager, nameof(UIManager));
            hasRequiredManagers &= ValidateManager(cursorManager, nameof(CursorManager));
            hasRequiredManagers &= ValidateManager(buildingManager, nameof(BuildingManager));
            hasRequiredManagers &= ValidateManager(playerBalanceManager, nameof(PlayerBalanceManager));
            hasRequiredManagers &= ValidateManager(phoneManager, nameof(PhoneManager));
            hasRequiredManagers &= ValidateManager(playerManager, nameof(PlayerManager));
            hasRequiredManagers &= ValidateManager(cameraManager, nameof(CameraManager));
            hasRequiredManagers &= ValidateManager(itemDeliveryManager, nameof(ItemDeliveryManager));
            hasRequiredManagers &= ValidateManager(inHandManager, nameof(InHandManager));
            hasRequiredManagers &= ValidateManager(shopPurchaseService, nameof(ShopPurchaseService));
            hasRequiredManagers &= ValidateManager(shopUIController, nameof(ShopUIController));
            hasRequiredManagers &= ValidateManager(slotMachineManager, nameof(SlotMachineManager));
            hasRequiredManagers &= ValidateManager(cashDeskManager, nameof(CashDeskManager));

            return hasRequiredManagers;
        }

        private bool HasRequiredSceneReferences()
        {
            bool hasRequiredSceneReferences = true;

            if (playerSpawnPoint == null)
            {
                Debug.LogError($"{nameof(GameManager)} is missing Player Spawn Point scene reference.", this);
                hasRequiredSceneReferences = false;
            }

            if (itemDeliveryPad == null)
            {
                Debug.LogError($"{nameof(GameManager)} is missing Item Delivery Pad scene reference.", this);
                hasRequiredSceneReferences = false;
            }

            if (cashDeskQueueStartPoint == null)
            {
                Debug.LogError($"{nameof(GameManager)} is missing Cash Desk Queue Start Point scene reference.", this);
                hasRequiredSceneReferences = false;
            }

            if (cashDeskTicketPlacementPoint == null)
            {
                Debug.LogError($"{nameof(GameManager)} is missing Cash Desk Ticket Placement Point scene reference.", this);
                hasRequiredSceneReferences = false;
            }

            return hasRequiredSceneReferences;
        }

        private T SpawnManager<T>(T prefab, string managerName) where T : MonoBehaviour
        {
            if (prefab == null)
            {
                Debug.LogError($"{nameof(GameManager)} is missing {managerName} prefab.", this);
                return null;
            }

            T instance = Instantiate(prefab);
            instance.name = managerName;

            return instance;
        }

        private bool ValidateManager<T>(T manager, string managerName) where T : MonoBehaviour
        {
            if (manager != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(GameManager)} cannot initialize because {managerName} is missing.", this);
            return false;
        }
    }
}