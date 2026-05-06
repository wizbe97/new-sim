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

        [Header("Purchase")]
        [SerializeField, Min(0)] private int price = 100;
        [SerializeField] private bool isUniqueItem;

        public string ItemId => itemId;
        public string ItemName => itemName;
        public string ItemDescription => itemDescription;
        public StoreItemType ItemType => itemType;
        public Sprite ItemIcon => itemIcon;
        public int Price => price;
        public bool IsUniqueItem => isUniqueItem;

        // Backwards-compatible property.
        // Your current UIManager uses DisplayName, so this keeps older code safe.
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
        }
#endif
    }
}