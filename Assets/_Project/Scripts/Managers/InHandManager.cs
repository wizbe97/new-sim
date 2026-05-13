using Project.Input;
using Project.Placement;
using Project.Player;
using Project.Shop;
using Project.SlotMachines;
using UnityEngine;

namespace Project.Hands
{
    public sealed class InHandManager : MonoBehaviour
    {
        [Header("Box Hold Transform")]
        [SerializeField] private Vector3 heldBoxLocalPosition = new Vector3(0f, -0.35f, 1f);
        [SerializeField] private Vector3 heldBoxLocalEulerAngles = Vector3.zero;

        [Header("Box Unpacking")]
        [SerializeField] private float unpackForwardOffset = 1.25f;
        [SerializeField] private float unpackUpOffset = -0.25f;
        [SerializeField] private bool beginPlacementWhenBoxOpened = true;

        [Header("Boxing Furniture")]
        [SerializeField] private bool holdBoxAfterPackingFurniture = true;

        private FirstPersonController player;
        private PlayerInputHandler input;
        private BuildingManager buildingManager;

        private Transform heldBoxPoint;
        private DeliveryBox heldBox;
        private CarryablePhysics heldBoxPhysics;
        private Vector3 heldBoxOriginalLocalScale = Vector3.one;

        private InHandItemType currentItemType = InHandItemType.None;
        private int handStateStartedFrame = -1;

        public InHandItemType CurrentItemType => currentItemType;
        public bool IsHoldingSomething => currentItemType != InHandItemType.None;
        public bool IsHoldingBox => currentItemType == InHandItemType.DeliveryBox && heldBox != null;
        public bool IsHoldingFurniture => currentItemType == InHandItemType.Furniture && buildingManager != null && buildingManager.IsBuilding;
        public DeliveryBox HeldBox => heldBox;

        public void Initialize(FirstPersonController newPlayer, BuildingManager newBuildingManager)
        {
            if (newPlayer == null)
            {
                Debug.LogError($"{nameof(InHandManager)} cannot initialize because player is missing.", this);
                return;
            }

            if (newBuildingManager == null)
            {
                Debug.LogError($"{nameof(InHandManager)} cannot initialize because BuildingManager is missing.", this);
                return;
            }

            player = newPlayer;
            buildingManager = newBuildingManager;

            input = player.GetComponent<PlayerInputHandler>();

            if (input == null)
            {
                Debug.LogError($"{nameof(InHandManager)} could not find {nameof(PlayerInputHandler)} on player.", player);
                return;
            }

            CreateHeldBoxPoint();
            SubscribeToInput();
        }

        private void OnDestroy()
        {
            UnsubscribeFromInput();
        }

        private void LateUpdate()
        {
            ApplyHeldBoxPointTransform();

            if (!IsHoldingBox)
            {
                return;
            }

            SnapHeldBoxToHoldPoint();
        }

        public bool TryPickupBox(DeliveryBox deliveryBox)
        {
            if (deliveryBox == null)
            {
                Debug.LogWarning($"{nameof(InHandManager)} tried to pick up a null delivery box.", this);
                return false;
            }

            if (IsHoldingSomething)
            {
                Debug.Log("Cannot pick up box because the player is already holding something.", this);
                return false;
            }

            if (!deliveryBox.CanCarry)
            {
                Debug.LogWarning($"{deliveryBox.name} cannot be carried.", deliveryBox);
                return false;
            }

            if (heldBoxPoint == null)
            {
                Debug.LogError($"{nameof(InHandManager)} cannot pick up box because Held Box Point is missing.", this);
                return false;
            }

            heldBox = deliveryBox;
            heldBoxOriginalLocalScale = heldBox.transform.localScale;

            heldBoxPhysics = heldBox.GetComponent<CarryablePhysics>();

            if (heldBoxPhysics == null)
            {
                heldBoxPhysics = heldBox.gameObject.AddComponent<CarryablePhysics>();
            }

            SetHandState(InHandItemType.DeliveryBox);

            heldBoxPhysics.DisableForCarry();

            heldBox.transform.SetParent(heldBoxPoint, false);
            SnapHeldBoxToHoldPoint();

            Debug.Log($"Picked up {heldBox.name}.", heldBox);
            return true;
        }

        public bool TryPickupFurniture(FurnitureItem furniture)
        {
            if (furniture == null)
            {
                Debug.LogWarning($"{nameof(InHandManager)} tried to pick up null furniture.", this);
                return false;
            }

            if (IsHoldingSomething)
            {
                Debug.Log("Cannot pick up furniture because the player is already holding something.", this);
                return false;
            }

            if (buildingManager == null)
            {
                Debug.LogError($"{nameof(InHandManager)} cannot pick up furniture because BuildingManager is missing.", this);
                return false;
            }

            bool started = buildingManager.BeginBuilding(furniture);

            if (!started)
            {
                return false;
            }
            NotifySlotMachinesPickedUpForPlacement(furniture);

            SetHandState(InHandItemType.Furniture);

            Debug.Log($"Picked up furniture for placement: {furniture.name}.", furniture);
            return true;
        }

        public void DropHeldBox()
        {
            if (!IsHoldingBox)
            {
                return;
            }

            DeliveryBox boxToDrop = heldBox;
            CarryablePhysics boxPhysics = heldBoxPhysics;

            boxToDrop.transform.SetParent(null, true);

            if (boxPhysics != null)
            {
                boxPhysics.RestoreAfterCarry();
                boxPhysics.EnableDroppedPhysics();
            }

            Debug.Log($"Dropped {boxToDrop.name}.", boxToDrop);

            ClearBoxState();
            ClearHandState();
        }

        public void OpenHeldBox()
        {
            if (!IsHoldingBox)
            {
                return;
            }

            DeliveryBox boxToOpen = heldBox;

            if (!boxToOpen.CanOpen)
            {
                Debug.LogWarning($"{boxToOpen.name} cannot be opened.", boxToOpen);
                return;
            }

            Vector3 spawnPosition = GetUnpackedItemSpawnPosition();
            Quaternion spawnRotation = GetUnpackedItemSpawnRotation();

            GameObject unpackedObject = boxToOpen.OpenBox(spawnPosition, spawnRotation);

            ClearBoxState();
            ClearHandState();

            if (unpackedObject == null || !beginPlacementWhenBoxOpened)
            {
                return;
            }

            FurnitureItem furnitureItem = unpackedObject.GetComponentInChildren<FurnitureItem>(true);

            if (furnitureItem == null)
            {
                Debug.LogWarning(
                    $"{unpackedObject.name} was unpacked, but it does not have a {nameof(FurnitureItem)} component.",
                    unpackedObject
                );

                return;
            }

            TryPickupFurniture(furnitureItem);
        }

        public void TryPlaceHeldFurniture()
        {
            if (!IsHoldingFurniture)
            {
                return;
            }

            FurnitureItem furniture = buildingManager.CurrentFurniture;

            bool placed = buildingManager.TryPlaceCurrentFurniture();

            if (!placed)
            {
                return;
            }

            NotifySlotMachinesPlacedAfterPlacement(furniture);

            ClearHandState();
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

            ClearHandState();
        }

        public void RotateHeldFurniture()
        {
            if (!IsHoldingFurniture)
            {
                return;
            }

            buildingManager.RotateCurrentFurniture();
        }

        public void PackHeldFurnitureIntoBox()
        {
            if (!IsHoldingFurniture)
            {
                return;
            }

            FurnitureItem furniture = buildingManager.CurrentFurniture;

            if (furniture == null)
            {
                return;
            }

            StoreItemSO storeItem = GetStoreItemForFurniture(furniture);

            if (storeItem == null)
            {
                Debug.LogWarning(
                    $"{furniture.name} cannot be boxed because it has no StoreItemSO assigned.",
                    furniture
                );

                return;
            }

            if (storeItem.DeliveryBoxPrefab == null)
            {
                Debug.LogWarning(
                    $"{storeItem.ItemName} cannot be boxed because it has no Delivery Box Prefab assigned.",
                    storeItem
                );

                return;
            }

            FurnitureItem boxedFurniture = buildingManager.TakeCurrentFurnitureForBoxing();

            if (boxedFurniture == null)
            {
                return;
            }

            Vector3 boxSpawnPosition = boxedFurniture.transform.position;
            Quaternion boxSpawnRotation = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);

            DeliveryBox newBox = Instantiate(
                storeItem.DeliveryBoxPrefab,
                boxSpawnPosition,
                boxSpawnRotation
            );

            newBox.Initialize(storeItem);

            if (newBox.GetComponent<CarryablePhysics>() == null)
            {
                newBox.gameObject.AddComponent<CarryablePhysics>();
            }

            Destroy(boxedFurniture.gameObject);

            ClearHandState();

            Debug.Log(
                $"Packed {storeItem.ItemName} into box prefab '{storeItem.DeliveryBoxPrefab.name}'.",
                newBox
            );

            if (holdBoxAfterPackingFurniture)
            {
                TryPickupBox(newBox);
            }
        }

        private void SubscribeToInput()
        {
            input.InteractPressed += HandleInteractPressed;
            input.RotatePressed += HandleRotatePressed;
            input.PackPressed += HandlePackPressed;
            input.CancelPressed += HandleCancelPressed;
        }

        private void UnsubscribeFromInput()
        {
            if (input == null)
            {
                return;
            }

            input.InteractPressed -= HandleInteractPressed;
            input.RotatePressed -= HandleRotatePressed;
            input.PackPressed -= HandlePackPressed;
            input.CancelPressed -= HandleCancelPressed;
        }

        private void HandleInteractPressed()
        {
            if (Time.frameCount == handStateStartedFrame)
            {
                return;
            }

            switch (currentItemType)
            {
                case InHandItemType.DeliveryBox:
                    DropHeldBox();
                    break;

                case InHandItemType.Furniture:
                    TryPlaceHeldFurniture();
                    break;
            }
        }

        private void HandleRotatePressed()
        {
            switch (currentItemType)
            {
                case InHandItemType.DeliveryBox:
                    OpenHeldBox();
                    break;

                case InHandItemType.Furniture:
                    RotateHeldFurniture();
                    break;
            }
        }

        private void HandlePackPressed()
        {
            if (currentItemType == InHandItemType.Furniture)
            {
                PackHeldFurnitureIntoBox();
            }
        }

        private void HandleCancelPressed()
        {
            switch (currentItemType)
            {
                case InHandItemType.DeliveryBox:
                    DropHeldBox();
                    break;

                case InHandItemType.Furniture:
                    CancelHeldFurniture();
                    break;
            }
        }

        private void SetHandState(InHandItemType itemType)
        {
            currentItemType = itemType;
            handStateStartedFrame = Time.frameCount;
        }

        private void ClearHandState()
        {
            currentItemType = InHandItemType.None;
            handStateStartedFrame = -1;
        }

        private void CreateHeldBoxPoint()
        {
            if (player == null || player.CameraTarget == null)
            {
                Debug.LogError($"{nameof(InHandManager)} cannot create Held Box Point because player Camera Target is missing.", this);
                return;
            }

            GameObject holdPointObject = new GameObject("HeldBoxPoint");
            heldBoxPoint = holdPointObject.transform;
            heldBoxPoint.SetParent(player.CameraTarget, false);

            ApplyHeldBoxPointTransform();
        }

        private void ApplyHeldBoxPointTransform()
        {
            if (heldBoxPoint == null)
            {
                return;
            }

            heldBoxPoint.localPosition = heldBoxLocalPosition;
            heldBoxPoint.localRotation = Quaternion.Euler(heldBoxLocalEulerAngles);
            heldBoxPoint.localScale = Vector3.one;
        }

        private void SnapHeldBoxToHoldPoint()
        {
            if (heldBox == null)
            {
                return;
            }

            heldBox.transform.localPosition = Vector3.zero;
            heldBox.transform.localRotation = Quaternion.identity;
            heldBox.transform.localScale = heldBoxOriginalLocalScale;
        }

        private Vector3 GetUnpackedItemSpawnPosition()
        {
            if (player == null || player.CameraTarget == null)
            {
                return heldBox != null ? heldBox.transform.position : transform.position;
            }

            return player.CameraTarget.position +
                   player.CameraTarget.forward * unpackForwardOffset +
                   Vector3.up * unpackUpOffset;
        }

        private Quaternion GetUnpackedItemSpawnRotation()
        {
            if (player == null)
            {
                return Quaternion.identity;
            }

            return Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);
        }

        private void ClearBoxState()
        {
            heldBox = null;
            heldBoxPhysics = null;
            heldBoxOriginalLocalScale = Vector3.one;
        }

        private StoreItemSO GetStoreItemForFurniture(FurnitureItem furniture)
        {
            return furniture != null ? furniture.StoreItem : null;
        }

        private void NotifySlotMachinesPickedUpForPlacement(FurnitureItem furniture)
        {
            if (furniture == null)
            {
                return;
            }

            SlotMachine[] slotMachines = furniture.GetComponentsInChildren<SlotMachine>(true);

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

            SlotMachine[] slotMachines = furniture.GetComponentsInChildren<SlotMachine>(true);

            for (int i = 0; i < slotMachines.Length; i++)
            {
                slotMachines[i].HandlePlacedAfterPlacement();
            }
        }
    }
}