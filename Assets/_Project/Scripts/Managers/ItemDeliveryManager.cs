using Project.Shop;
using UnityEngine;

namespace Project.Managers
{
    public sealed class ItemDeliveryManager : MonoBehaviour
    {
        [Header("Delivery Location")]
        [SerializeField] private Transform deliveryPad;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnHeight = 20f;
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;
        [SerializeField] private bool randomizeHorizontalOffset = true;
        [SerializeField, Min(0f)] private float randomHorizontalRadius = 0.4f;

        [Header("Physics Settings")]
        [SerializeField] private bool ensureRigidbody = true;
        [SerializeField] private bool ensureCollider = true;
        [SerializeField] private float deliveredBoxMass = 3f;
        [SerializeField] private float deliveredBoxDrag = 0.1f;
        [SerializeField] private float deliveredBoxAngularDrag = 0.05f;
        [SerializeField] private Vector3 randomTorqueRange = new Vector3(3f, 3f, 3f);

        public void Initialize(Transform deliveryPadTransform)
        {
            if (deliveryPadTransform == null)
            {
                Debug.LogError($"{nameof(ItemDeliveryManager)} cannot initialize because Delivery Pad is missing.", this);
                return;
            }

            deliveryPad = deliveryPadTransform;
        }

        public bool DeliverItem(StoreItemSO storeItem)
        {
            if (storeItem == null)
            {
                Debug.LogError($"{nameof(ItemDeliveryManager)} cannot deliver item because StoreItemSO is missing.", this);
                return false;
            }

            if (storeItem.PlaceablePrefab == null)
            {
                Debug.LogError(
                    $"{nameof(ItemDeliveryManager)} cannot deliver {storeItem.ItemName} because it has no Placeable Prefab assigned.",
                    storeItem
                );

                return false;
            }

            if (storeItem.DeliveryBoxPrefab == null)
            {
                Debug.LogError(
                    $"{nameof(ItemDeliveryManager)} cannot deliver {storeItem.ItemName} because it has no Delivery Box Prefab assigned.",
                    storeItem
                );

                return false;
            }

            if (deliveryPad == null)
            {
                Debug.LogError($"{nameof(ItemDeliveryManager)} cannot deliver {storeItem.ItemName} because Delivery Pad is missing.", this);
                return false;
            }

            Vector3 spawnPosition = GetSpawnPosition();
            Quaternion spawnRotation = Quaternion.identity;

            DeliveryBox deliveredBox = Instantiate(storeItem.DeliveryBoxPrefab, spawnPosition, spawnRotation);
            deliveredBox.name = $"DeliveryBox_{storeItem.ItemName}";
            deliveredBox.Initialize(storeItem);

            ConfigurePhysics(deliveredBox.gameObject);
            ApplyDeliveryGimmick(deliveredBox.GetComponent<Rigidbody>());

            Debug.Log($"Delivered boxed item: {storeItem.ItemName} to {spawnPosition}.", deliveredBox);

            return true;
        }

        private Vector3 GetSpawnPosition()
        {
            Vector3 basePosition = deliveryPad.position;
            Vector3 finalOffset = spawnOffset;

            if (randomizeHorizontalOffset)
            {
                Vector2 randomCircle = Random.insideUnitCircle * randomHorizontalRadius;
                finalOffset.x += randomCircle.x;
                finalOffset.z += randomCircle.y;
            }

            return new Vector3(
                basePosition.x + finalOffset.x,
                spawnHeight + finalOffset.y,
                basePosition.z + finalOffset.z
            );
        }

        private Rigidbody ConfigurePhysics(GameObject deliveredObject)
        {
            Rigidbody rigidbody = deliveredObject.GetComponent<Rigidbody>();

            if (rigidbody == null && ensureRigidbody)
            {
                rigidbody = deliveredObject.AddComponent<Rigidbody>();
            }

            if (rigidbody != null)
            {
                rigidbody.mass = deliveredBoxMass;
                rigidbody.drag = deliveredBoxDrag;
                rigidbody.angularDrag = deliveredBoxAngularDrag;
                rigidbody.useGravity = true;
                rigidbody.isKinematic = false;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            }

            Collider collider = deliveredObject.GetComponent<Collider>();

            if (collider == null && ensureCollider)
            {
                deliveredObject.AddComponent<BoxCollider>();
            }

            return rigidbody;
        }

        private void ApplyDeliveryGimmick(Rigidbody rigidbody)
        {
            if (rigidbody == null)
            {
                return;
            }

            Vector3 randomTorque = new Vector3(
                Random.Range(-randomTorqueRange.x, randomTorqueRange.x),
                Random.Range(-randomTorqueRange.y, randomTorqueRange.y),
                Random.Range(-randomTorqueRange.z, randomTorqueRange.z)
            );

            rigidbody.AddTorque(randomTorque, ForceMode.Impulse);
        }
    }
}