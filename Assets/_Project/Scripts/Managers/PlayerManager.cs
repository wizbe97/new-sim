using Project.Player;
using Project.Spawning;
using UnityEngine;

namespace Project.Managers
{
    public sealed class PlayerManager : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private FirstPersonController playerPrefab;

        [Header("Spawn")]
        [SerializeField] private PlayerSpawnPoint fallbackSpawnPoint;

        private FirstPersonController currentPlayer;

        public FirstPersonController CurrentPlayer => currentPlayer;

        public void Initialize(UIManager uiManager, PlacementManager placementManager)
        {
            SpawnPlayer(uiManager, placementManager);
        }

        public void SetFallbackSpawnPoint(PlayerSpawnPoint spawnPoint)
        {
            fallbackSpawnPoint = spawnPoint;
        }

        private void SpawnPlayer(UIManager uiManager, PlacementManager placementManager)
        {
            if (playerPrefab == null)
            {
                Debug.LogError($"{nameof(PlayerManager)} is missing a player prefab.");
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
                spawnRotation
            );

            currentPlayer.name = "Player";

            PlayerInteractionController interactionController =
                currentPlayer.GetComponent<PlayerInteractionController>();

            if (interactionController != null)
            {
                interactionController.Initialize(uiManager, placementManager);
            }
            else
            {
                Debug.LogWarning($"{nameof(PlayerManager)} spawned a player without a {nameof(PlayerInteractionController)}.");
            }
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
                Debug.LogWarning($"{nameof(PlayerManager)} could not find a PlayerSpawnPoint. Player will spawn at world origin.");
            }

            return sceneSpawnPoint;
        }
    }
}