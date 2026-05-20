using Project.Player;
using Project.Shop;
using UnityEngine;

namespace Project.Hands
{
    [DisallowMultipleComponent]
    public sealed class HeldBoxController : MonoBehaviour
    {
        [Header("Box Hold Transform")]
        [SerializeField] private Vector3 heldBoxLocalPosition = new Vector3(0f, -0.35f, 1f);
        [SerializeField] private Vector3 heldBoxLocalEulerAngles = Vector3.zero;

        [Header("Box Unpacking")]
        [SerializeField] private float unpackForwardOffset = 1.25f;
        [SerializeField] private float unpackUpOffset = -0.25f;
        [SerializeField] private bool beginPlacementWhenBoxOpened = true;

        private FirstPersonController player;
        private Transform heldBoxPoint;
        private DeliveryBox heldBox;
        private CarryablePhysics heldBoxPhysics;
        private Vector3 heldBoxOriginalLocalScale = Vector3.one;

        public bool IsHoldingBox => heldBox != null;
        public DeliveryBox HeldBox => heldBox;
        public bool BeginPlacementWhenBoxOpened => beginPlacementWhenBoxOpened;

        public void Initialize(FirstPersonController newPlayer)
        {
            if (newPlayer == null)
            {
                Debug.LogError($"{nameof(HeldBoxController)} cannot initialize because player is missing.", this);
                return;
            }

            player = newPlayer;
            CreateHeldBoxPoint();
        }

        public void TickLate()
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
                Debug.LogWarning($"{nameof(HeldBoxController)} tried to pick up a null delivery box.", this);
                return false;
            }

            if (!deliveryBox.CanCarry)
            {
                Debug.LogWarning($"{deliveryBox.name} cannot be carried.", deliveryBox);
                return false;
            }

            if (heldBoxPoint == null)
            {
                Debug.LogError($"{nameof(HeldBoxController)} cannot pick up box because Held Box Point is missing.", this);
                return false;
            }

            heldBox = deliveryBox;
            heldBoxOriginalLocalScale = heldBox.transform.localScale;

            heldBoxPhysics = heldBox.GetComponent<CarryablePhysics>();

            if (heldBoxPhysics == null)
            {
                heldBoxPhysics = heldBox.gameObject.AddComponent<CarryablePhysics>();
            }

            heldBoxPhysics.DisableForCarry();

            heldBox.transform.SetParent(heldBoxPoint, false);
            SnapHeldBoxToHoldPoint();

            Debug.Log($"Picked up {heldBox.name}.", heldBox);
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
        }

        public GameObject OpenHeldBox()
        {
            if (!IsHoldingBox)
            {
                return null;
            }

            DeliveryBox boxToOpen = heldBox;

            if (!boxToOpen.CanOpen)
            {
                Debug.LogWarning($"{boxToOpen.name} cannot be opened.", boxToOpen);
                return null;
            }

            Vector3 spawnPosition = GetUnpackedItemSpawnPosition();
            Quaternion spawnRotation = GetUnpackedItemSpawnRotation();

            GameObject unpackedObject = boxToOpen.OpenBox(spawnPosition, spawnRotation);

            ClearBoxState();

            return unpackedObject;
        }

        private void CreateHeldBoxPoint()
        {
            if (player == null || player.CameraTarget == null)
            {
                Debug.LogError($"{nameof(HeldBoxController)} cannot create Held Box Point because player Camera Target is missing.", this);
                return;
            }

            if (heldBoxPoint != null)
            {
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            unpackForwardOffset = Mathf.Max(0f, unpackForwardOffset);
        }
#endif
    }
}