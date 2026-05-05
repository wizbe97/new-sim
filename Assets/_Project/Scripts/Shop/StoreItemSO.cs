using UnityEngine;

namespace Project.Shop
{
    [CreateAssetMenu(
        fileName = "StoreItem",
        menuName = "Project/Shop/Store Item")]
    public sealed class StoreItemSO : ScriptableObject
    {
        [Header("Item Info")]
        [SerializeField] private string displayName = "New Store Item";
        [SerializeField, Min(0)] private int price = 100;

        public string DisplayName => displayName;
        public int Price => price;
    }
}