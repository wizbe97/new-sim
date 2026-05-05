using Project.Spawning;
using UnityEngine;

namespace Project.Managers
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Manager Prefabs")]
        [SerializeField] private PlayerManager playerManagerPrefab;
        [SerializeField] private CameraManager cameraManagerPrefab;
        [SerializeField] private UIManager uiManagerPrefab;
        [SerializeField] private CursorManager cursorManagerPrefab;
        [SerializeField] private PlacementManager placementManagerPrefab;

        [Header("Scene References")]
        [SerializeField] private PlayerSpawnPoint playerSpawnPoint;

        private PlayerManager playerManager;
        private CameraManager cameraManager;
        private UIManager uiManager;
        private CursorManager cursorManager;
        private PlacementManager placementManager;

        public PlayerManager PlayerManager => playerManager;
        public CameraManager CameraManager => cameraManager;
        public UIManager UIManager => uiManager;
        public CursorManager CursorManager => cursorManager;
        public PlacementManager PlacementManager => placementManager;

        private void Awake()
        {
            SpawnManagers();
            InitializeManagers();
        }

        private void SpawnManagers()
        {
            playerManager = SpawnManager(playerManagerPrefab, "PlayerManager");
            cameraManager = SpawnManager(cameraManagerPrefab, "CameraManager");
            uiManager = SpawnManager(uiManagerPrefab, "UIManager");
            cursorManager = SpawnManager(cursorManagerPrefab, "CursorManager");
            placementManager = SpawnManager(placementManagerPrefab, "PlacementManager");
        }

        private void InitializeManagers()
        {
            if (uiManager == null)
            {
                Debug.LogError($"{nameof(GameManager)} cannot initialize because UIManager is missing.");
                return;
            }

            uiManager.Initialize();

            if (placementManager == null)
            {
                Debug.LogError($"{nameof(GameManager)} cannot initialize because PlacementManager is missing.");
                return;
            }

            if (playerManager == null)
            {
                Debug.LogError($"{nameof(GameManager)} cannot initialize because PlayerManager is missing.");
                return;
            }

            playerManager.SetFallbackSpawnPoint(playerSpawnPoint);
            playerManager.Initialize(uiManager, placementManager);

            if (cameraManager == null)
            {
                Debug.LogError($"{nameof(GameManager)} cannot initialize because CameraManager is missing.");
                return;
            }

            cameraManager.Initialize(playerManager.CurrentPlayer);

            placementManager.Initialize(playerManager.CurrentPlayer);

            if (cursorManager == null)
            {
                Debug.LogError($"{nameof(GameManager)} cannot initialize because CursorManager is missing.");
                return;
            }

            cursorManager.Initialize(uiManager);
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
    }
}