using Project.Input;
using Project.Placement;
using Project.Player;
using Project.Managers;
using Project.Shop;
using UnityEngine;

namespace Project.Hands
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HeldBoxController))]
    [RequireComponent(typeof(HeldFurnitureController))]
    public sealed class InHandManager : MonoBehaviour
    {
        private FirstPersonController player;
        private PlayerInputHandler input;
        private BuildingManager buildingManager;
        private CasinoProgressionManager casinoProgressionManager;

        private HeldBoxController heldBoxController;
        private HeldFurnitureController heldFurnitureController;

        private InHandItemType currentItemType = InHandItemType.None;
        private int handStateStartedFrame = -1;

        public InHandItemType CurrentItemType => currentItemType;
        public bool IsHoldingSomething => currentItemType != InHandItemType.None;

        public bool IsHoldingBox =>
            currentItemType == InHandItemType.DeliveryBox &&
            heldBoxController != null &&
            heldBoxController.IsHoldingBox;

        public bool IsHoldingFurniture =>
            currentItemType == InHandItemType.Furniture &&
            heldFurnitureController != null &&
            heldFurnitureController.IsHoldingFurniture;

        public DeliveryBox HeldBox =>
            heldBoxController != null ? heldBoxController.HeldBox : null;

        public void Initialize(
            FirstPersonController newPlayer,
            BuildingManager newBuildingManager,
            CasinoProgressionManager newCasinoProgressionManager)
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

            if (newCasinoProgressionManager == null)
            {
                Debug.LogError($"{nameof(InHandManager)} cannot initialize because CasinoProgressionManager is missing.", this);
                return;
            }

            player = newPlayer;
            buildingManager = newBuildingManager;
            casinoProgressionManager = newCasinoProgressionManager;

            input = player.GetComponent<PlayerInputHandler>();

            if (input == null)
            {
                Debug.LogError($"{nameof(InHandManager)} could not find {nameof(PlayerInputHandler)} on player.", player);
                return;
            }

            CacheControllers();

            heldBoxController.Initialize(player);
            heldFurnitureController.Initialize(
                player,
                buildingManager,
                casinoProgressionManager);

            SubscribeToInput();
        }

        private void OnDestroy()
        {
            UnsubscribeFromInput();
        }

        private void LateUpdate()
        {
            if (heldBoxController == null)
            {
                return;
            }

            heldBoxController.TickLate();
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

            if (heldBoxController == null)
            {
                Debug.LogError($"{nameof(InHandManager)} cannot pick up box because HeldBoxController is missing.", this);
                return false;
            }

            bool pickedUp = heldBoxController.TryPickupBox(deliveryBox);

            if (!pickedUp)
            {
                return false;
            }

            SetHandState(InHandItemType.DeliveryBox);
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

            if (heldFurnitureController == null)
            {
                Debug.LogError($"{nameof(InHandManager)} cannot pick up furniture because HeldFurnitureController is missing.", this);
                return false;
            }

            bool pickedUp = heldFurnitureController.TryPickupFurniture(furniture);

            if (!pickedUp)
            {
                return false;
            }

            SetHandState(InHandItemType.Furniture);
            return true;
        }

        public void DropHeldBox()
        {
            if (!IsHoldingBox)
            {
                return;
            }

            heldBoxController.DropHeldBox();
            ClearHandState();
        }

        public void OpenHeldBox()
        {
            if (!IsHoldingBox)
            {
                return;
            }

            GameObject unpackedObject = heldBoxController.OpenHeldBox();

            if (unpackedObject == null)
            {
                return;
            }

            ClearHandState();

            if (!heldBoxController.BeginPlacementWhenBoxOpened)
            {
                return;
            }

            FurnitureItem furnitureItem =
                unpackedObject.GetComponentInChildren<FurnitureItem>(true);

            if (furnitureItem == null)
            {
                Debug.LogWarning(
                    $"{unpackedObject.name} was unpacked, but it does not have a {nameof(FurnitureItem)} component.",
                    unpackedObject);

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

            bool placed = heldFurnitureController.TryPlaceHeldFurniture();

            if (!placed)
            {
                return;
            }

            ClearHandState();
        }

        public void CancelHeldFurniture()
        {
            if (!IsHoldingFurniture)
            {
                return;
            }

            heldFurnitureController.CancelHeldFurniture();
            ClearHandState();
        }

        public void RotateHeldFurniture()
        {
            if (!IsHoldingFurniture)
            {
                return;
            }

            heldFurnitureController.RotateHeldFurniture();
        }

        public void PackHeldFurnitureIntoBox()
        {
            if (!IsHoldingFurniture)
            {
                return;
            }

            bool packed = heldFurnitureController.TryPackHeldFurnitureIntoBox(
                out DeliveryBox newBox,
                out bool shouldHoldBoxAfterPacking);

            if (!packed)
            {
                return;
            }

            ClearHandState();

            if (shouldHoldBoxAfterPacking && newBox != null)
            {
                TryPickupBox(newBox);
            }
        }

        private void CacheControllers()
        {
            heldBoxController = GetComponent<HeldBoxController>();

            if (heldBoxController == null)
            {
                heldBoxController = gameObject.AddComponent<HeldBoxController>();
            }

            heldFurnitureController = GetComponent<HeldFurnitureController>();

            if (heldFurnitureController == null)
            {
                heldFurnitureController = gameObject.AddComponent<HeldFurnitureController>();
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
    }
}