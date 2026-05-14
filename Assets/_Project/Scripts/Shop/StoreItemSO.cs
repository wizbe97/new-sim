using UnityEngine;

namespace Project.Shop
{
    [CreateAssetMenu(
        fileName = "StoreItem",
        menuName = "Project/Shop/Store Item")]
    public sealed class StoreItemSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId = "new_item_id";

        [Header("Display Info")]
        [SerializeField] private string itemName = "New Store Item";
        [SerializeField, TextArea(2, 5)] private string itemDescription = "Item description.";
        [SerializeField] private StoreItemType itemType = StoreItemType.Misc;
        [SerializeField] private Sprite itemIcon;

        [Header("World Item")]
        [Tooltip("The actual furniture/item prefab that should appear when the delivery box is opened.")]
        [SerializeField] private GameObject placeablePrefab;

        [Tooltip("The cardboard box prefab that should fall onto the delivery pad when this item is bought.")]
        [SerializeField] private DeliveryBox deliveryBoxPrefab;

        [Header("Purchase")]
        [SerializeField, Min(0)] private int price = 100;
        [SerializeField] private bool isUniqueItem;

        [Header("Progression")]
        [SerializeField, Min(1)] private int requiredCasinoLevel = 1;

        public string ItemId => itemId;
        public string ItemName => itemName;
        public string ItemDescription => itemDescription;
        public StoreItemType ItemType => itemType;
        public Sprite ItemIcon => itemIcon;
        public GameObject PlaceablePrefab => placeablePrefab;
        public DeliveryBox DeliveryBoxPrefab => deliveryBoxPrefab;
        public int Price => price;
        public bool IsUniqueItem => isUniqueItem;
        public int RequiredCasinoLevel => requiredCasinoLevel;

        public string DisplayName => itemName;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                itemId = name;
            }

            itemId = itemId.Trim().ToLowerInvariant().Replace(" ", "_");

            if (string.IsNullOrWhiteSpace(itemName))
            {
                itemName = name;
            }

            requiredCasinoLevel = Mathf.Max(1, requiredCasinoLevel);
        }
#endif
    }
}