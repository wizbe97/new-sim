using Project.Spawning;
using UnityEngine;

namespace Project.Managers
{
    public sealed class SceneReferenceProvider : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private bool usePlayerInScene = true;
        [SerializeField] private PlayerSpawnPoint playerSpawnPoint;

        [Header("Item Delivery")]
        [SerializeField] private bool useItemDeliveryInScene;
        [SerializeField] private Transform itemDeliveryPad;

        [Header("Cash Desk")]
        [SerializeField] private bool useCashDeskInScene;
        [SerializeField] private Transform cashDeskQueueStartPoint;
        [SerializeField] private Transform cashDeskTicketPlacementPoint;

        [Tooltip("Where staff stand while assigned to cash desk duty.")]
        [SerializeField] private Transform cashDeskStaffServicePoint;

        [Header("Staff")]
        [Tooltip("Where idle staff should stand. This is a scene reference and should not be assigned directly on staff prefabs.")]
        [SerializeField] private Transform staffIdleStandPoint;

        public bool UsePlayerInScene => usePlayerInScene;
        public PlayerSpawnPoint PlayerSpawnPoint => playerSpawnPoint;

        public bool UseItemDeliveryInScene => useItemDeliveryInScene;
        public Transform ItemDeliveryPad => itemDeliveryPad;

        public bool UseCashDeskInScene => useCashDeskInScene;
        public Transform CashDeskQueueStartPoint => cashDeskQueueStartPoint;
        public Transform CashDeskTicketPlacementPoint => cashDeskTicketPlacementPoint;
        public Transform CashDeskStaffServicePoint => cashDeskStaffServicePoint;

        public Transform StaffIdleStandPoint => staffIdleStandPoint;

        private void OnValidate()
        {
            if (!usePlayerInScene)
            {
                playerSpawnPoint = null;
            }

            if (!useItemDeliveryInScene)
            {
                itemDeliveryPad = null;
            }

            if (!useCashDeskInScene)
            {
                cashDeskQueueStartPoint = null;
                cashDeskTicketPlacementPoint = null;
                cashDeskStaffServicePoint = null;
            }
        }
    }
}