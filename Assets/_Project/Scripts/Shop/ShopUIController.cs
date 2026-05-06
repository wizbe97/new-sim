using Project.Managers;
using UnityEngine;

namespace Project.Shop
{
    public sealed class ShopUIController : MonoBehaviour
    {
        private UIManager uiManager;
        private ShopPurchaseService shopPurchaseService;

        private bool isInitialized;

        public void Initialize(
            UIManager newUIManager,
            ShopPurchaseService newShopPurchaseService)
        {
            if (newUIManager == null)
            {
                Debug.LogError($"{nameof(ShopUIController)} cannot initialize because UIManager is missing.", this);
                return;
            }

            if (newShopPurchaseService == null)
            {
                Debug.LogError($"{nameof(ShopUIController)} cannot initialize because ShopPurchaseService is missing.", this);
                return;
            }

            if (isInitialized)
            {
                UnsubscribeFromEvents();
            }

            uiManager = newUIManager;
            shopPurchaseService = newShopPurchaseService;

            uiManager.SetShopOwnershipProvider(shopPurchaseService.IsUniqueItemOwned);

            SubscribeToEvents();

            isInitialized = true;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (uiManager != null)
            {
                uiManager.ShopItemBuyRequested += HandleShopItemBuyRequested;
            }

            if (shopPurchaseService != null)
            {
                shopPurchaseService.ItemPurchased += HandleItemPurchased;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (uiManager != null)
            {
                uiManager.ShopItemBuyRequested -= HandleShopItemBuyRequested;
            }

            if (shopPurchaseService != null)
            {
                shopPurchaseService.ItemPurchased -= HandleItemPurchased;
            }
        }

        private void HandleShopItemBuyRequested(StoreItemSO storeItem)
        {
            if (shopPurchaseService == null)
            {
                Debug.LogError($"{nameof(ShopUIController)} cannot buy item because ShopPurchaseService is missing.", this);
                return;
            }

            shopPurchaseService.TryBuyItem(storeItem);
        }

        private void HandleItemPurchased(StoreItemSO storeItem)
        {
            if (uiManager == null)
            {
                return;
            }

            uiManager.RefreshPhoneShopItems();
        }
    }
}