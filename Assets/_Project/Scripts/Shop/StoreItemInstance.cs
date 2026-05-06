using UnityEngine;

namespace Project.Shop
{
    public sealed class StoreItemInstance : MonoBehaviour
    {
        [Header("Source Item")]
        [SerializeField] private StoreItemSO storeItem;

        public StoreItemSO StoreItem => storeItem;

        public void Initialize(StoreItemSO item)
        {
            storeItem = item;
        }
    }
}