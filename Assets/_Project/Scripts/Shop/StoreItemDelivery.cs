using Project.Shop;
using UnityEngine;

namespace Project.Shop
{
    public sealed class StoreItemDelivery : MonoBehaviour
    {
        [Header("Item Data")]
        [SerializeField] private StoreItemSO storeItem;

        public StoreItemSO StoreItem => storeItem;

        public void Initialize(StoreItemSO item)
        {
            storeItem = item;
            name = item != null
                ? $"Delivered_{item.ItemName}"
                : "Delivered_Item";
        }
    }
}