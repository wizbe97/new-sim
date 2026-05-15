using System;
using Project.UI.Phone;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Project.Shop
{
    public sealed class ShopItemButtonView : MonoBehaviour, IPhoneCatalogueItemView
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Image itemIconImage;
        [SerializeField] private Button buyButton;
        [SerializeField] private TextMeshProUGUI buyButtonText;

        [Header("Optional Lock Display")]
        [Tooltip("Optional. If assigned, displays lock text such as 'Requires Level 3'.")]
        [SerializeField] private TextMeshProUGUI lockStatusText;

        [Header("Formatting")]
        [SerializeField] private string currencyPrefix = "£";
        [SerializeField] private string buyText = "Buy";
        [SerializeField] private string ownedText = "Owned";
        [SerializeField] private string lockedTextFormat = "Requires Level {0}";

        private StoreItemSO storeItem;
        private PhonePageContext context;

        private bool isOwned;
        private bool isUnlocked = true;
        private int requiredCasinoLevel = 1;

        public event Action<StoreItemSO> BuyPressed;

        public StoreItemSO StoreItem => storeItem;

        private void Awake()
        {
            if (buyButton != null)
            {
                buyButton.onClick.AddListener(HandleBuyButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(HandleBuyButtonClicked);
            }
        }

        public void Initialize(Object item, PhonePageContext newContext)
        {
            StoreItemSO newStoreItem = item as StoreItemSO;

            if (newStoreItem == null)
            {
                Debug.LogError($"{nameof(ShopItemButtonView)} on {name} expected a {nameof(StoreItemSO)} item.", this);
                return;
            }

            context = newContext;

            Initialize(
                newStoreItem,
                context != null && context.IsShopItemOwned(newStoreItem),
                context == null || context.IsShopItemUnlocked(newStoreItem),
                newStoreItem.RequiredCasinoLevel);
        }

        public void Dispose()
        {
            context = null;
            BuyPressed = null;
        }

        public void Initialize(StoreItemSO item, bool owned)
        {
            Initialize(
                item,
                owned,
                unlocked: true,
                newRequiredCasinoLevel: item != null ? item.RequiredCasinoLevel : 1);
        }

        public void Initialize(
            StoreItemSO item,
            bool owned,
            bool unlocked,
            int newRequiredCasinoLevel)
        {
            storeItem = item;
            isOwned = owned;
            isUnlocked = unlocked;
            requiredCasinoLevel = Mathf.Max(1, newRequiredCasinoLevel);

            Refresh();
        }

        public void SetOwned(bool owned)
        {
            isOwned = owned;
            RefreshPurchaseState();
        }

        public void SetPurchaseState(bool owned, bool unlocked, int newRequiredCasinoLevel)
        {
            isOwned = owned;
            isUnlocked = unlocked;
            requiredCasinoLevel = Mathf.Max(1, newRequiredCasinoLevel);

            RefreshPurchaseState();
        }

        public void Refresh()
        {
            if (storeItem == null)
            {
                Debug.LogError($"{nameof(ShopItemButtonView)} on {name} cannot refresh because Store Item is missing.", this);
                return;
            }

            if (context != null)
            {
                isOwned = context.IsShopItemOwned(storeItem);
                isUnlocked = context.IsShopItemUnlocked(storeItem);
            }

            if (itemNameText != null)
            {
                itemNameText.text = storeItem.ItemName;
            }

            if (priceText != null)
            {
                priceText.text = $"{currencyPrefix}{storeItem.Price:N0}";
            }

            if (itemIconImage != null)
            {
                itemIconImage.sprite = storeItem.ItemIcon;
                itemIconImage.enabled = storeItem.ItemIcon != null;
                itemIconImage.preserveAspect = true;
            }

            RefreshPurchaseState();
        }

        private void RefreshPurchaseState()
        {
            bool shouldDisableBecauseOwned = storeItem != null && storeItem.IsUniqueItem && isOwned;
            bool shouldDisableBecauseLocked = !isUnlocked;
            bool canBuy = !shouldDisableBecauseOwned && !shouldDisableBecauseLocked;

            if (buyButton != null)
            {
                buyButton.interactable = canBuy;
            }

            if (buyButtonText != null)
            {
                if (shouldDisableBecauseOwned)
                {
                    buyButtonText.text = ownedText;
                }
                else if (shouldDisableBecauseLocked)
                {
                    buyButtonText.text = string.Format(lockedTextFormat, requiredCasinoLevel);
                }
                else
                {
                    buyButtonText.text = buyText;
                }
            }

            if (lockStatusText != null)
            {
                lockStatusText.gameObject.SetActive(shouldDisableBecauseLocked);
                lockStatusText.text = shouldDisableBecauseLocked
                    ? string.Format(lockedTextFormat, requiredCasinoLevel)
                    : string.Empty;
            }
        }

        private void HandleBuyButtonClicked()
        {
            if (storeItem == null)
            {
                Debug.LogError($"{nameof(ShopItemButtonView)} on {name} cannot buy because Store Item is missing.", this);
                return;
            }

            if (!isUnlocked)
            {
                Debug.Log($"{storeItem.ItemName} requires casino level {requiredCasinoLevel}.", storeItem);
                return;
            }

            if (storeItem.IsUniqueItem && isOwned)
            {
                Debug.Log($"{storeItem.ItemName} is unique and has already been purchased.", storeItem);
                return;
            }

            if (context != null)
            {
                context.RequestShopItemPurchase(storeItem);
                return;
            }

            BuyPressed?.Invoke(storeItem);
        }
    }
}