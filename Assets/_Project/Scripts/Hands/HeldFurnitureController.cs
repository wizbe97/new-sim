using Project.Managers;
using Project.Placement;
using Project.Player;
using Project.Progression;
using Project.Shop;
using Project.SlotMachines;
using UnityEngine;

namespace Project.Hands
{
    [DisallowMultipleComponent]
    public sealed class HeldFurnitureController : MonoBehaviour
    {
        [Header("Boxing Furniture")]
        [SerializeField] private bool holdBoxAfterPackingFurniture = true;

        private FirstPersonController player;
        private BuildingManager buildingManager;
        private CasinoProgressionManager casinoProgressionManager;

        public bool IsHoldingFurniture =>
            buildingManager != null &&
            buildingManager.IsBuilding;

        public FurnitureItem CurrentFurniture =>
            buildingManager != null ? buildingManager.CurrentFurniture : null;

        public void Initialize(
            FirstPersonController newPlayer,
            BuildingManager newBuildingManager,
            CasinoProgressionManager newCasinoProgressionManager)
        {
            if (newPlayer == null)
            {
                Debug.LogError($"{nameof(HeldFurnitureController)} cannot initialize because player is missing.", this);
                return;
            }

            if (newBuildingManager == null)
            {
                Debug.LogError($"{nameof(HeldFurnitureController)} cannot initialize because BuildingManager is missing.", this);
                return;
            }

            if (newCasinoProgressionManager == null)
            {
                Debug.LogError($"{nameof(HeldFurnitureController)} cannot initialize because CasinoProgressionManager is missing.", this);
                return;
            }

            player = newPlayer;
            buildingManager = newBuildingManager;
            casinoProgressionManager = newCasinoProgressionManager;
        }

        public bool TryPickupFurniture(FurnitureItem furniture)
        {
            if (furniture == null)
            {
                Debug.LogWarning($"{nameof(HeldFurnitureController)} tried to pick up null furniture.", this);
                return false;
            }

            if (buildingManager == null)
            {
                Debug.LogError($"{nameof(HeldFurnitureController)} cannot pick up furniture because BuildingManager is missing.", this);
                return false;
            }

            bool started = buildingManager.BeginBuilding(furniture);

            if (!started)
            {
                return false;
            }

            NotifySlotMachinesPickedUpForPlacement(furniture);

            Debug.Log($"Picked up furniture for placement: {furniture.name}.", furniture);
            return true;
        }

        public bool TryPlaceHeldFurniture()
        {
            if (!IsHoldingFurniture)
            {
                return false;
            }

            FurnitureItem furniture = buildingManager.CurrentFurniture;

            bool placed = buildingManager.TryPlaceCurrentFurniture();

            if (!placed)
            {
                return false;
            }

            NotifySlotMachinesPlacedAfterPlacement(furniture);
            AwardPlacementXpIfNeeded(furniture);

            return true;
        }

        public void CancelHeldFurniture()
        {
            if (!IsHoldingFurniture)
            {
                return;
            }

            FurnitureItem furniture = buildingManager.CurrentFurniture;

            buildingManager.CancelCurrentFurniture();

            NotifySlotMachinesPlacedAfterPlacement(furniture);
        }

        public void RotateHeldFurniture()
        {
            if (!IsHoldingFurniture)
            {
                return;
            }

            buildingManager.RotateCurrentFurniture();
        }

        public bool TryPackHeldFurnitureIntoBox(
            out DeliveryBox newBox,
            out bool shouldHoldBoxAfterPacking)
        {
            newBox = null;
            shouldHoldBoxAfterPacking = false;

            if (!IsHoldingFurniture)
            {
                return false;
            }

            FurnitureItem furniture = buildingManager.CurrentFurniture;

            if (furniture == null)
            {
                return false;
            }

            StoreItemSO storeItem = GetStoreItemForFurniture(furniture);

            if (storeItem == null)
            {
                Debug.LogWarning(
                    $"{furniture.name} cannot be boxed because it has no StoreItemSO assigned.",
                    furniture);

                return false;
            }

            if (storeItem.DeliveryBoxPrefab == null)
            {
                Debug.LogWarning(
                    $"{storeItem.ItemName} cannot be boxed because it has no Delivery Box Prefab assigned.",
                    storeItem);

                return false;
            }

            FurnitureItem boxedFurniture = buildingManager.TakeCurrentFurnitureForBoxing();

            if (boxedFurniture == null)
            {
                return false;
            }

            Vector3 boxSpawnPosition = boxedFurniture.transform.position;
            Quaternion boxSpawnRotation = GetBoxSpawnRotation();

            newBox = Instantiate(
                storeItem.DeliveryBoxPrefab,
                boxSpawnPosition,
                boxSpawnRotation);

            newBox.Initialize(storeItem);

            if (newBox.GetComponent<CarryablePhysics>() == null)
            {
                newBox.gameObject.AddComponent<CarryablePhysics>();
            }

            Destroy(boxedFurniture.gameObject);

            shouldHoldBoxAfterPacking = holdBoxAfterPackingFurniture;

            Debug.Log(
                $"Packed {storeItem.ItemName} into box prefab '{storeItem.DeliveryBoxPrefab.name}'.",
                newBox);

            return true;
        }

        private Quaternion GetBoxSpawnRotation()
        {
            if (player == null)
            {
                return Quaternion.identity;
            }

            return Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);
        }

        private StoreItemSO GetStoreItemForFurniture(FurnitureItem furniture)
        {
            return furniture != null ? furniture.StoreItem : null;
        }

        private void AwardPlacementXpIfNeeded(FurnitureItem furniture)
        {
            if (furniture == null || casinoProgressionManager == null)
            {
                return;
            }

            if (!furniture.TryMarkPlacementXpAwarded())
            {
                return;
            }

            casinoProgressionManager.AddConfiguredXp(CasinoXpSource.ItemPlaced);
        }

        private void NotifySlotMachinesPickedUpForPlacement(FurnitureItem furniture)
        {
            if (furniture == null)
            {
                return;
            }

            SlotMachine[] slotMachines =
                furniture.GetComponentsInChildren<SlotMachine>(true);

            for (int i = 0; i < slotMachines.Length; i++)
            {
                slotMachines[i].HandlePickedUpForPlacement();
            }
        }

        private void NotifySlotMachinesPlacedAfterPlacement(FurnitureItem furniture)
        {
            if (furniture == null)
            {
                return;
            }

            SlotMachine[] slotMachines =
                furniture.GetComponentsInChildren<SlotMachine>(true);

            for (int i = 0; i < slotMachines.Length; i++)
            {
                slotMachines[i].HandlePlacedAfterPlacement();
            }
        }
    }
}