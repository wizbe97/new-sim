using Project.Hands;
using Project.Placement;
using Project.Shop;
using UnityEngine;

namespace Project.Managers
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

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
        [SerializeField] private CasinoProgressionManager casinoProgressionManagerPrefab;
        [SerializeField] private StaffManager staffManagerPrefab;

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
        private CasinoProgressionManager casinoProgressionManager;
        private StaffManager staffManager;

        private bool hasInitializedGlobalManagers;
        private bool hasInitializedPlayerManagers;

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
        public CasinoProgressionManager CasinoProgressionManager => casinoProgressionManager;
        public StaffManager StaffManager => staffManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SpawnManagers();

            if (!HasRequiredManagers())
            {
                Debug.LogError($"{nameof(GameManager)} failed to initialize because one or more required managers are missing.", this);
                return;
            }

            InitializeGlobalManagers();
        }

        public void BindScene(SceneReferenceProvider sceneReferences)
        {
            if (sceneReferences == null)
            {
                Debug.LogError($"{nameof(GameManager)} cannot bind scene because SceneReferenceProvider is missing.", this);
                return;
            }

            if (!HasRequiredManagers())
            {
                Debug.LogError($"{nameof(GameManager)} cannot bind scene because one or more required managers are missing.", this);
                return;
            }

            BindPlayerSceneReferences(sceneReferences);
            BindDeliverySceneReferences(sceneReferences);
            BindCashDeskSceneReferences(sceneReferences);

            if (slotMachineManager != null)
            {
                slotMachineManager.RefreshSceneMachines();
            }
        }

        private void BindPlayerSceneReferences(SceneReferenceProvider sceneReferences)
        {
            if (!sceneReferences.UsePlayerInScene)
            {
                playerManager.SetCurrentPlayerActive(false);
                return;
            }

            if (sceneReferences.PlayerSpawnPoint == null)
            {
                Debug.LogError($"{nameof(GameManager)} cannot bind player because Player Spawn Point is missing.", sceneReferences);
                return;
            }

            playerManager.SpawnOrMovePlayer(
                uiManager,
                buildingManager,
                sceneReferences.PlayerSpawnPoint);

            playerManager.SetCurrentPlayerActive(true);

            InitializePlayerManagersIfNeeded();

            if (cameraManager != null)
            {
                cameraManager.Initialize(playerManager.CurrentPlayer);
            }

            if (buildingManager != null)
            {
                buildingManager.Initialize(playerManager.CurrentPlayer);
            }
        }

        private void BindDeliverySceneReferences(SceneReferenceProvider sceneReferences)
        {
            if (!sceneReferences.UseItemDeliveryInScene)
            {
                itemDeliveryManager.ClearDeliveryPad();
                staffManager.ClearDeliveryPad();
                return;
            }

            if (sceneReferences.ItemDeliveryPad == null)
            {
                Debug.LogError($"{nameof(GameManager)} cannot bind item delivery because Item Delivery Pad is missing.", sceneReferences);
                itemDeliveryManager.ClearDeliveryPad();
                staffManager.ClearDeliveryPad();
                return;
            }

            itemDeliveryManager.Initialize(sceneReferences.ItemDeliveryPad);
            staffManager.InitializeDeliveryPad(sceneReferences.ItemDeliveryPad);
        }

        private void BindCashDeskSceneReferences(SceneReferenceProvider sceneReferences)
        {
            if (!sceneReferences.UseCashDeskInScene)
            {
                cashDeskManager.ClearSceneBindings();
                return;
            }

            if (sceneReferences.CashDeskQueueStartPoint == null ||
                sceneReferences.CashDeskTicketPlacementPoint == null)
            {
                Debug.LogError($"{nameof(GameManager)} cannot bind cash desk because cash desk scene references are missing.", sceneReferences);
                cashDeskManager.ClearSceneBindings();
                return;
            }

            cashDeskManager.Initialize(
                playerBalanceManager,
                sceneReferences.CashDeskQueueStartPoint,
                sceneReferences.CashDeskTicketPlacementPoint,
                casinoProgressionManager);
        }

        private void InitializeGlobalManagers()
        {
            if (hasInitializedGlobalManagers)
            {
                return;
            }

            hasInitializedGlobalManagers = true;

            casinoProgressionManager.Initialize();

            playerBalanceManager.Initialize(casinoProgressionManager);

            shopPurchaseService.Initialize(
                playerBalanceManager,
                itemDeliveryManager,
                casinoProgressionManager);

            uiManager.Initialize(
                playerBalanceManager,
                casinoProgressionManager);

            staffManager.Initialize(
                playerBalanceManager,
                casinoProgressionManager,
                uiManager);

            shopUIController.Initialize(
                uiManager,
                shopPurchaseService,
                casinoProgressionManager);

            cursorManager.Initialize(uiManager);
        }

        private void InitializePlayerManagersIfNeeded()
        {
            if (hasInitializedPlayerManagers)
            {
                return;
            }

            if (playerManager.CurrentPlayer == null)
            {
                Debug.LogError($"{nameof(GameManager)} cannot initialize player managers because no player exists.", this);
                return;
            }

            hasInitializedPlayerManagers = true;

            cameraManager.Initialize(playerManager.CurrentPlayer);
            buildingManager.Initialize(playerManager.CurrentPlayer);

            phoneManager.Initialize(
                playerManager.CurrentPlayer,
                uiManager,
                cursorManager);

            inHandManager.Initialize(
                playerManager.CurrentPlayer,
                buildingManager,
                casinoProgressionManager);

            slotMachineManager.Initialize(
                playerManager.CurrentPlayer,
                playerBalanceManager,
                cursorManager,
                casinoProgressionManager);
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
            casinoProgressionManager = SpawnManager(casinoProgressionManagerPrefab, nameof(CasinoProgressionManager));
            staffManager = SpawnManager(staffManagerPrefab, nameof(StaffManager));
        }

        private T SpawnManager<T>(T prefab, string managerName) where T : MonoBehaviour
        {
            if (prefab == null)
            {
                Debug.LogError($"{nameof(GameManager)} is missing {managerName} prefab.", this);
                return null;
            }

            T instance = Instantiate(prefab, transform);
            instance.name = managerName;

            return instance;
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
            hasRequiredManagers &= ValidateManager(casinoProgressionManager, nameof(CasinoProgressionManager));
            hasRequiredManagers &= ValidateManager(staffManager, nameof(StaffManager));

            return hasRequiredManagers;
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