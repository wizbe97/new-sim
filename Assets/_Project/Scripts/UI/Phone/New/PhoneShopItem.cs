using System;
using Project.Shop;

namespace Project.UI.Phone
{
    [Serializable]
    public sealed class PhoneShopItem : PhonePageItem
    {
        [UnityEngine.SerializeField] private StoreItemSO storeItem;

        public override PhonePageItemActionType ActionType => PhonePageItemActionType.BuyStoreItem;
        public override StoreItemSO StoreItem => storeItem;

        public override string GetTitle()
        {
            return storeItem != null ? storeItem.ItemName : "Missing Store Item";
        }

        public override string GetDescription()
        {
            return storeItem != null ? storeItem.ItemDescription : string.Empty;
        }

        public override UnityEngine.Sprite GetIcon()
        {
            return storeItem != null ? storeItem.ItemIcon : null;
        }

        public override string GetPrimaryText()
        {
            return storeItem != null ? $"Buy £{storeItem.Price:N0}" : "Missing Item";
        }

        public override string GetSecondaryText()
        {
            if (storeItem == null)
            {
                return string.Empty;
            }

            return storeItem.IsUniqueItem ? "Unique Item" : string.Empty;
        }
    }
}