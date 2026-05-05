using Project.Spawning;
using UnityEngine;

namespace Project.Managers
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Manager Prefabs")]
        [SerializeField] private UIManager uiManagerPrefab;
        [SerializeField] private CursorManager cursorManagerPrefab;
        [SerializeField] private PlacementManager placementManagerPrefab;
        [SerializeField] private PlayerBalanceManager playerBalanceManagerPrefab;
        [SerializeField] private PhoneManager phoneManagerPrefab;
        [SerializeField] private PlayerManager playerManagerPrefab;
        [SerializeField] private CameraManager cameraManagerPrefab;

        [Header("Scene References")]
        [SerializeField] private PlayerSpawnPoint playerSpawnPoint;

        private UIManager uiManager;
        private CursorManager cursorManager;
        private PlacementManager placementManager;
        private PlayerBalanceManager playerBalanceManager;
        private PhoneManager phoneManager;
        private PlayerManager playerManager;
        private CameraManager cameraManager;

        public UIManager UIManager => uiManager;
        public CursorManager CursorManager => cursorManager;
        public PlacementManager PlacementManager => placementManager;
        public PlayerBalanceManager PlayerBalanceManager => playerBalanceManager;
        public PhoneManager PhoneManager => phoneManager;
        public PlayerManager PlayerManager => playerManager;
        public CameraManager CameraManager => cameraManager;

        private void Awake()
        {
            SpawnManagers();

            if (!HasRequiredManagers())
            {
                Debug.LogError($"{nameof(GameManager)} failed to initialize because one or more required managers are missing.");
                return;
            }

            InitializeManagers();
        }

        private void SpawnManagers()
        {
            uiManager = SpawnManager(uiManagerPrefab, nameof(UIManager));
            cursorManager = SpawnManager(cursorManagerPrefab, nameof(CursorManager));
            placementManager = SpawnManager(placementManagerPrefab, nameof(PlacementManager));
            playerBalanceManager = SpawnManager(playerBalanceManagerPrefab, nameof(PlayerBalanceManager));
            phoneManager = SpawnManager(phoneManagerPrefab, nameof(PhoneManager));
            playerManager = SpawnManager(playerManagerPrefab, nameof(PlayerManager));
            cameraManager = SpawnManager(cameraManagerPrefab, nameof(CameraManager));
        }

        private bool HasRequiredManagers()
        {
            bool hasRequiredManagers = true;

            hasRequiredManagers &= ValidateManager(uiManager, nameof(UIManager));
            hasRequiredManagers &= ValidateManager(cursorManager, nameof(CursorManager));
            hasRequiredManagers &= ValidateManager(placementManager, nameof(PlacementManager));
            hasRequiredManagers &= ValidateManager(playerBalanceManager, nameof(PlayerBalanceManager));
            hasRequiredManagers &= ValidateManager(phoneManager, nameof(PhoneManager));
            hasRequiredManagers &= ValidateManager(playerManager, nameof(PlayerManager));
            hasRequiredManagers &= ValidateManager(cameraManager, nameof(CameraManager));

            return hasRequiredManagers;
        }

        private void InitializeManagers()
        {
            InitializeEconomy();
            InitializeUI();
            InitializePlayer();
            InitializeCamera();
            InitializePlacement();
            InitializeCursor();
            InitializePhone();
        }

        private void InitializeEconomy()
        {
            playerBalanceManager.Initialize();
        }

        private void InitializeUI()
        {
            uiManager.Initialize(playerBalanceManager);
        }

        private void InitializePlayer()
        {
            playerManager.SetFallbackSpawnPoint(playerSpawnPoint);
            playerManager.Initialize(uiManager, placementManager);
        }

        private void InitializeCamera()
        {
            cameraManager.Initialize(playerManager.CurrentPlayer);
        }

        private void InitializePlacement()
        {
            placementManager.Initialize(playerManager.CurrentPlayer);
        }

        private void InitializeCursor()
        {
            cursorManager.Initialize(uiManager);
        }

        private void InitializePhone()
        {
            phoneManager.Initialize(playerManager.CurrentPlayer, uiManager, cursorManager);
        }

        private T SpawnManager<T>(T prefab, string managerName) where T : MonoBehaviour
        {
            if (prefab == null)
            {
                Debug.LogError($"{nameof(GameManager)} is missing {managerName} prefab.");
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

            Debug.LogError($"{nameof(GameManager)} cannot initialize because {managerName} is missing.");
            return false;
        }
    }
}