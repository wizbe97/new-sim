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
                Debug.LogError($"{nameof(CameraManager)} cannot initialize without a player.", this);
                return;
            }

            CreateOrBindFirstPersonCamera(player);
        }

        private void CreateOrBindFirstPersonCamera(FirstPersonController player)
        {
            if (firstPersonCameraPrefab == null)
            {
                Debug.LogError($"{nameof(CameraManager)} is missing a Cinemachine virtual camera prefab.", this);
                return;
            }

            if (player.CameraTarget == null)
            {
                Debug.LogError($"{nameof(CameraManager)} cannot find player's CameraTarget.", player);
                return;
            }

            if (activeCamera == null)
            {
                activeCamera = Instantiate(firstPersonCameraPrefab, transform);
                activeCamera.name = "PlayerCinemachineCamera";
                activeCamera.Priority = firstPersonCameraPriority;
            }

            activeCamera.Follow = player.CameraTarget;
        }
    }
}