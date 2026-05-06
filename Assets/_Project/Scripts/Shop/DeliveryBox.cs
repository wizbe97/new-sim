using Project.Placement;
using UnityEngine;

namespace Project.Shop
{
    public sealed class DeliveryBox : MonoBehaviour
    {
        [Header("Stored Item")]
        [SerializeField] private StoreItemSO storedItem;

        [Header("Opening")]
        [SerializeField] private bool destroyBoxWhenOpened = true;

        private bool hasBeenOpened;

        public StoreItemSO StoredItem => storedItem;
        public bool HasBeenOpened => hasBeenOpened;

        public bool CanOpen =>
            !hasBeenOpened &&
            storedItem != null &&
            storedItem.PlaceablePrefab != null;

        public void Initialize(StoreItemSO item)
        {
            storedItem = item;
            hasBeenOpened = false;

            if (storedItem != null)
            {
                name = $"DeliveryBox_{storedItem.ItemName}";
            }
        }

        public GameObject OpenBox(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (hasBeenOpened)
            {
                Debug.Log($"{name} has already been opened.", this);
                return null;
            }

            if (storedItem == null)
            {
                Debug.LogError($"{nameof(DeliveryBox)} on {name} cannot open because Stored Item is missing.", this);
                return null;
            }

            if (storedItem.PlaceablePrefab == null)
            {
                Debug.LogError(
                    $"{nameof(DeliveryBox)} on {name} cannot open {storedItem.ItemName} because Placeable Prefab is missing.",
                    storedItem
                );

                return null;
            }

            hasBeenOpened = true;

            GameObject spawnedItem = Instantiate(
                storedItem.PlaceablePrefab,
                spawnPosition,
                spawnRotation
            );

            spawnedItem.name = storedItem.ItemName;

            StoreItemInstance instance = spawnedItem.GetComponent<StoreItemInstance>();

            if (instance == null)
            {
                instance = spawnedItem.AddComponent<StoreItemInstance>();
            }

            instance.Initialize(storedItem);

            FurnitureItem furnitureItem = spawnedItem.GetComponent<FurnitureItem>();

            if (furnitureItem != null)
            {
                furnitureItem.SetStoreItem(storedItem);
            }

            Debug.Log($"Opened {name} and unpacked {storedItem.ItemName}.", this);

            if (destroyBoxWhenOpened)
            {
                Destroy(gameObject);
            }

            return spawnedItem;
        }
    }
}