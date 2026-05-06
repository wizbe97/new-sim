using System.Collections.Generic;
using Project.Input;
using Project.Placement;
using Project.Player;
using Project.Shop;
using UnityEngine;

namespace Project.Managers
{
    public sealed class BoxCarryManager : MonoBehaviour
    {
        [Header("Hold Position")]
        [SerializeField] private Vector3 heldLocalPosition = new Vector3(0f, -0.35f, 1f);
        [SerializeField] private Vector3 heldLocalEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 heldLocalScale = Vector3.one;

        [Header("Opening")]
        [SerializeField] private float unpackForwardOffset = 1.25f;
        [SerializeField] private float unpackUpOffset = -0.25f;
        [SerializeField] private bool beginPlacementWhenOpened = true;

        [Header("Boxing")]
        [SerializeField] private bool holdBoxAfterPacking = true;

        private FirstPersonController player;
        private PlacementManager placementManager;
        private PlayerInputHandler input;

        private Transform heldItemPoint;
        private DeliveryBox heldBox;

        private readonly List<RigidbodyState> rigidbodyStates = new();
        private readonly List<ColliderState> colliderStates = new();

        private int carryStartedFrame = -1;

        public bool IsHoldingBox => heldBox != null;
        public DeliveryBox HeldBox => heldBox;

        public void Initialize(FirstPersonController newPlayer, PlacementManager newPlacementManager)
        {
            if (newPlayer == null)
            {
                Debug.LogError($"{nameof(BoxCarryManager)} cannot initialize because player is missing.", this);
                return;
            }

            if (newPlacementManager == null)
            {
                Debug.LogError($"{nameof(BoxCarryManager)} cannot initialize because PlacementManager is missing.", this);
                return;
            }

            player = newPlayer;
            placementManager = newPlacementManager;

            input = player.GetComponent<PlayerInputHandler>();

            if (input == null)
            {
                Debug.LogError($"{nameof(BoxCarryManager)} could not find {nameof(PlayerInputHandler)} on player.", player);
                return;
            }

            CreateHeldItemPoint();

            input.InteractPressed += HandleInteractPressed;
            input.RotatePressed += HandleRotatePressed;
            input.PackPressed += HandlePackPressed;
            input.CancelPressed += HandleCancelPressed;
        }

        private void OnDestroy()
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

        private void LateUpdate()
        {
            ApplyHeldItemPointTransform();

            if (!IsHoldingBox)
            {
                return;
            }

            SnapHeldBoxToHoldPoint();
        }

        public bool BeginCarry(DeliveryBox deliveryBox)
        {
            if (deliveryBox == null)
            {
                Debug.LogWarning($"{nameof(BoxCarryManager)} tried to carry a null delivery box.", this);
                return false;
            }

            if (IsHoldingBox)
            {
                Debug.Log("Already holding a box.", this);
                return false;
            }

            if (placementManager != null && placementManager.IsPlacing)
            {
                Debug.Log("Cannot pick up a box while placing furniture.", this);
                return false;
            }

            if (heldItemPoint == null)
            {
                Debug.LogError($"{nameof(BoxCarryManager)} cannot carry box because Held Item Point is missing.", this);
                return false;
            }

            heldBox = deliveryBox;
            carryStartedFrame = Time.frameCount;

            CachePhysicsState();
            DisableHeldBoxPhysics();

            heldBox.transform.SetParent(heldItemPoint, false);
            SnapHeldBoxToHoldPoint();

            Debug.Log($"Picked up {heldBox.name}. Left click to drop, R to open.", heldBox);

            return true;
        }

        public void DropHeldBox()
        {
            if (!IsHoldingBox)
            {
                return;
            }

            DeliveryBox boxToDrop = heldBox;

            boxToDrop.transform.SetParent(null, true);

            RestoreHeldBoxPhysics();
            ForceDroppedBoxPhysics(boxToDrop);

            Debug.Log($"Dropped {boxToDrop.name}.", boxToDrop);

            ClearHeldBoxReferences();
        }

        private void ForceDroppedBoxPhysics(DeliveryBox droppedBox)
        {
            if (droppedBox == null)
            {
                return;
            }

            Rigidbody[] rigidbodies = droppedBox.GetComponentsInChildren<Rigidbody>(true);

            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Rigidbody rb = rigidbodies[i];

                if (rb == null)
                {
                    continue;
                }

                rb.useGravity = true;
                rb.isKinematic = false;
                rb.detectCollisions = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.WakeUp();
            }

            Collider[] colliders = droppedBox.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider boxCollider = colliders[i];

                if (boxCollider == null)
                {
                    continue;
                }

                boxCollider.enabled = true;
            }
        }

        public void OpenHeldBox()
        {
            if (!IsHoldingBox)
            {
                return;
            }

            if (placementManager != null && placementManager.IsPlacing)
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

            ClearHeldBoxReferences();

            if (unpackedObject == null)
            {
                return;
            }

            if (!beginPlacementWhenOpened)
            {
                return;
            }

            FurnitureItem furnitureItem = unpackedObject.GetComponent<FurnitureItem>();

            if (furnitureItem == null)
            {
                Debug.LogWarning(
                    $"{unpackedObject.name} was unpacked, but it does not have a {nameof(FurnitureItem)} component. " +
                    "It cannot enter placement mode.",
                    unpackedObject
                );

                return;
            }

            placementManager.BeginPlacement(furnitureItem);
        }

        public void PackCurrentFurniture()
        {
            if (IsHoldingBox)
            {
                return;
            }

            if (placementManager == null || !placementManager.IsPlacing)
            {
                return;
            }

            FurnitureItem furniture = placementManager.CurrentFurniture;

            if (furniture == null)
            {
                return;
            }
            StoreItemSO storeItem = GetStoreItemForFurniture(furniture);

            if (storeItem == null)
            {
                Debug.LogWarning(
                    $"{furniture.name} cannot be boxed because it has no StoreItemSO assigned. " +
                    $"Assign one on {nameof(FurnitureItem)} or make sure it was unpacked from a delivery box.",
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

            FurnitureItem boxedFurniture = placementManager.TakeCurrentFurnitureForBoxing();

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

            Destroy(boxedFurniture.gameObject);

            Debug.Log($"Packed {storeItem.ItemName} back into a box.", newBox);

            if (holdBoxAfterPacking)
            {
                BeginCarry(newBox);
            }
        }

        private void CreateHeldItemPoint()
        {
            if (player == null || player.CameraTarget == null)
            {
                Debug.LogError($"{nameof(BoxCarryManager)} cannot create Held Item Point because player Camera Target is missing.", this);
                return;
            }

            GameObject holdPointObject = new GameObject("HeldItemPoint");
            heldItemPoint = holdPointObject.transform;

            heldItemPoint.SetParent(player.CameraTarget, false);
            ApplyHeldItemPointTransform();
        }

        private void ApplyHeldItemPointTransform()
        {
            if (heldItemPoint == null)
            {
                return;
            }

            heldItemPoint.localPosition = heldLocalPosition;
            heldItemPoint.localRotation = Quaternion.Euler(heldLocalEulerAngles);
            heldItemPoint.localScale = Vector3.one;
        }

        private void SnapHeldBoxToHoldPoint()
        {
            if (heldBox == null)
            {
                return;
            }

            heldBox.transform.localPosition = Vector3.zero;
            heldBox.transform.localRotation = Quaternion.identity;
            heldBox.transform.localScale = heldLocalScale;
        }

        private void CachePhysicsState()
        {
            rigidbodyStates.Clear();
            colliderStates.Clear();

            if (heldBox == null)
            {
                return;
            }

            Rigidbody[] rigidbodies = heldBox.GetComponentsInChildren<Rigidbody>(true);

            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Rigidbody rb = rigidbodies[i];

                if (rb == null)
                {
                    continue;
                }

                rigidbodyStates.Add(new RigidbodyState(rb));
            }

            Collider[] colliders = heldBox.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];

                if (collider == null)
                {
                    continue;
                }

                colliderStates.Add(new ColliderState(collider));
            }
        }

        private void DisableHeldBoxPhysics()
        {
            for (int i = 0; i < rigidbodyStates.Count; i++)
            {
                Rigidbody rb = rigidbodyStates[i].Rigidbody;

                if (rb == null)
                {
                    continue;
                }

                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true;
                rb.detectCollisions = false;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.interpolation = RigidbodyInterpolation.None;
            }

            for (int i = 0; i < colliderStates.Count; i++)
            {
                Collider collider = colliderStates[i].Collider;

                if (collider == null)
                {
                    continue;
                }

                collider.enabled = false;
            }
        }

        private void RestoreHeldBoxPhysics()
        {
            for (int i = 0; i < colliderStates.Count; i++)
            {
                colliderStates[i].Restore();
            }

            for (int i = 0; i < rigidbodyStates.Count; i++)
            {
                rigidbodyStates[i].Restore();
            }
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

        private void ClearHeldBoxReferences()
        {
            heldBox = null;
            carryStartedFrame = -1;
            rigidbodyStates.Clear();
            colliderStates.Clear();
        }

        private void HandleInteractPressed()
        {
            if (!IsHoldingBox)
            {
                return;
            }

            // Prevent the same left-click that picked up the box from instantly dropping it.
            if (Time.frameCount == carryStartedFrame)
            {
                return;
            }

            DropHeldBox();
        }

        private void HandleRotatePressed()
        {
            if (!IsHoldingBox)
            {
                return;
            }

            OpenHeldBox();
        }

        private void HandlePackPressed()
        {
            PackCurrentFurniture();
        }

        private void HandleCancelPressed()
        {
            if (!IsHoldingBox)
            {
                return;
            }

            DropHeldBox();
        }

        private readonly struct RigidbodyState
        {
            public Rigidbody Rigidbody { get; }
            private readonly bool useGravity;
            private readonly bool isKinematic;
            private readonly bool detectCollisions;
            private readonly CollisionDetectionMode collisionDetectionMode;
            private readonly RigidbodyInterpolation interpolation;

            public RigidbodyState(Rigidbody rigidbody)
            {
                Rigidbody = rigidbody;
                useGravity = rigidbody.useGravity;
                isKinematic = rigidbody.isKinematic;
                detectCollisions = rigidbody.detectCollisions;
                collisionDetectionMode = rigidbody.collisionDetectionMode;
                interpolation = rigidbody.interpolation;
            }

            public void Restore()
            {
                if (Rigidbody == null)
                {
                    return;
                }

                Rigidbody.useGravity = useGravity;
                Rigidbody.isKinematic = isKinematic;
                Rigidbody.detectCollisions = detectCollisions;
                Rigidbody.collisionDetectionMode = collisionDetectionMode;
                Rigidbody.interpolation = interpolation;
                Rigidbody.velocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;
            }
        }

        private readonly struct ColliderState
        {
            public Collider Collider { get; }
            private readonly bool enabled;

            public ColliderState(Collider collider)
            {
                Collider = collider;
                enabled = collider.enabled;
            }

            public void Restore()
            {
                if (Collider == null)
                {
                    return;
                }

                Collider.enabled = enabled;
            }
        }
        private StoreItemSO GetStoreItemForFurniture(FurnitureItem furniture)
        {
            if (furniture == null)
            {
                return null;
            }

            StoreItemInstance itemInstance = furniture.GetComponent<StoreItemInstance>();

            if (itemInstance != null && itemInstance.StoreItem != null)
            {
                return itemInstance.StoreItem;
            }

            if (furniture.StoreItem != null)
            {
                return furniture.StoreItem;
            }

            return null;
        }
    }

}
