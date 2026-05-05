using Cinemachine;
using Project.Player;
using UnityEngine;

namespace Project.Managers
{
    public sealed class CameraManager : MonoBehaviour
    {
        [Header("Cinemachine")]
        [SerializeField] private CinemachineVirtualCamera firstPersonCameraPrefab;

        [Header("Settings")]
        [SerializeField] private int firstPersonCameraPriority = 10;

        private CinemachineVirtualCamera activeCamera;

        public CinemachineVirtualCamera ActiveCamera => activeCamera;

        public void Initialize(FirstPersonController player)
        {
            if (player == null)
            {
                Debug.LogError($"{nameof(CameraManager)} cannot initialize without a player.");
                return;
            }

            CreateFirstPersonCamera(player);
        }

        private void CreateFirstPersonCamera(FirstPersonController player)
        {
            if (firstPersonCameraPrefab == null)
            {
                Debug.LogError($"{nameof(CameraManager)} is missing a Cinemachine virtual camera prefab.");
                return;
            }

            if (player.CameraTarget == null)
            {
                Debug.LogError($"{nameof(CameraManager)} cannot find player's CameraTarget.");
                return;
            }

            activeCamera = Instantiate(firstPersonCameraPrefab);
            activeCamera.name = "PlayerCinemachineCamera";

            activeCamera.Follow = player.CameraTarget;
            activeCamera.Priority = firstPersonCameraPriority;
        }
    }
}