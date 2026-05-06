using UnityEngine;

namespace Project.Shop
{
    public sealed class DeliveryBox : MonoBehaviour
    {
        [Header("Stored Item")]
        [SerializeField] private StoreItemSO storedItem;

        [Header("Opening")]
        [SerializeField] private Transform itemSpawnPoint;
        [SerializeField] private bool destroyBoxWhenOpened = true;
        [SerializeField] private Vector3 unpackedItemSpawnOffset = Vector3.up;

        private bool hasBeenOpened;

        public StoreItemSO StoredItem => storedItem;
        public bool HasBeenOpened => hasBeenOpened;

        public void Initialize(StoreItemSO item)
        {
            storedItem = item;

            if (storedItem != null)
            {
                name = $"DeliveryBox_{storedItem.ItemName}";
            }
        }

        public bool OpenBox()
        {
            if (hasBeenOpened)
            {
                Debug.Log($"{name} has already been opened.", this);
                return false;
            }

            if (storedItem == null)
            {
                Debug.LogError($"{nameof(DeliveryBox)} on {name} cannot open because Stored Item is missing.", this);
                return false;
            }

            if (storedItem.PlaceablePrefab == null)
            {
                Debug.LogError(
                    $"{nameof(DeliveryBox)} on {name} cannot open {storedItem.ItemName} because Placeable Prefab is missing.",
                    storedItem
                );

                return false;
            }

            hasBeenOpened = true;

            Vector3 spawnPosition = GetItemSpawnPosition();
            Quaternion spawnRotation = Quaternion.identity;

            GameObject spawnedItem = Instantiate(storedItem.PlaceablePrefab, spawnPosition, spawnRotation);
            spawnedItem.name = storedItem.ItemName;

            Debug.Log($"Opened {name} and unpacked {storedItem.ItemName}.", this);

            if (destroyBoxWhenOpened)
            {
                Destroy(gameObject);
            }

            return true;
        }

        private Vector3 GetItemSpawnPosition()
        {
            if (itemSpawnPoint != null)
            {
                return itemSpawnPoint.position;
            }

            return transform.position + unpackedItemSpawnOffset;
        }
    }
}