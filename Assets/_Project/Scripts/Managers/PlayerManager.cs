using Project.Placement;
using Project.Spawning;
using UnityEngine;

namespace Project.Managers
{
    public sealed class PlayerManager : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private Project.Player.FirstPersonController playerPrefab;

        [Header("Spawn")]
        [SerializeField] private PlayerSpawnPoint fallbackSpawnPoint;

        private Project.Player.FirstPersonController currentPlayer;

        public Project.Player.FirstPersonController CurrentPlayer => currentPlayer;

        public void Initialize(UIManager uiManager, BuildingManager buildingManager)
        {
            SpawnOrMovePlayer(uiManager, buildingManager, fallbackSpawnPoint);
        }

        public void SetFallbackSpawnPoint(PlayerSpawnPoint spawnPoint)
        {
            fallbackSpawnPoint = spawnPoint;
        }

        public void SpawnOrMovePlayer(
            UIManager uiManager,
            BuildingManager buildingManager,
            PlayerSpawnPoint spawnPoint)
        {
            fallbackSpawnPoint = spawnPoint;

            if (currentPlayer == null)
            {
                SpawnPlayer(uiManager, buildingManager);
                return;
            }

            MovePlayerToSpawnPoint(ResolveSpawnPoint());
        }

        public void SetCurrentPlayerActive(bool isActive)
        {
            if (currentPlayer == null)
            {
                return;
            }

            currentPlayer.gameObject.SetActive(isActive);
        }

        private void SpawnPlayer(UIManager uiManager, BuildingManager buildingManager)
        {
            if (playerPrefab == null)
            {
                Debug.LogError($"{nameof(PlayerManager)} is missing a player prefab.", this);
                return;
            }

            PlayerSpawnPoint spawnPoint = ResolveSpawnPoint();

            Vector3 spawnPosition = spawnPoint != null
                ? spawnPoint.Position
                : Vector3.zero;

            Quaternion spawnRotation = spawnPoint != null
                ? spawnPoint.Rotation
                : Quaternion.identity;

            currentPlayer = Instantiate(
                playerPrefab,
                spawnPosition,
                spawnRotation,
                transform);

            currentPlayer.name = "Player";

            Project.Player.PlayerInteractionController interactionController =
                currentPlayer.GetComponent<Project.Player.PlayerInteractionController>();

            if (interactionController != null)
            {
                interactionController.Initialize(uiManager, buildingManager);
            }
            else
            {
                Debug.LogWarning($"{nameof(PlayerManager)} spawned a player without a PlayerInteractionController.", currentPlayer);
            }
        }

        private void MovePlayerToSpawnPoint(PlayerSpawnPoint spawnPoint)
        {
            if (currentPlayer == null || spawnPoint == null)
            {
                return;
            }

            currentPlayer.transform.SetPositionAndRotation(
                spawnPoint.Position,
                spawnPoint.Rotation);
        }

        private PlayerSpawnPoint ResolveSpawnPoint()
        {
            if (fallbackSpawnPoint != null)
            {
                return fallbackSpawnPoint;
            }

            PlayerSpawnPoint sceneSpawnPoint = FindFirstObjectByType<PlayerSpawnPoint>();

            if (sceneSpawnPoint == null)
            {
                Debug.LogWarning($"{nameof(PlayerManager)} could not find a PlayerSpawnPoint. Player will spawn at world origin.", this);
            }

            return sceneSpawnPoint;
        }
    }
}