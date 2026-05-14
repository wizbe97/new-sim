using System;
using Project.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class ShopItemButtonView : MonoBehaviour
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
        [SerializeField] private string currencyPrefix = "$";
        [SerializeField] private string buyText = "Buy";
        [SerializeField] private string ownedText = "Owned";
        [SerializeField] private string lockedTextFormat = "Requires Level {0}";

        private StoreItemSO storeItem;
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
                Debug.LogError($"{nameof(ShopItemButtonView)} on {name} cannot refresh because Store Item is missing.");
                return;
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
                Debug.LogError($"{nameof(ShopItemButtonView)} on {name} cannot buy because Store Item is missing.");
                return;
            }

            if (!isUnlocked)
            {
                Debug.Log($"{storeItem.ItemName} requires casino level {requiredCasinoLevel}.");
                return;
            }

            if (storeItem.IsUniqueItem && isOwned)
            {
                Debug.Log($"{storeItem.ItemName} is unique and has already been purchased.");
                return;
            }

            BuyPressed?.Invoke(storeItem);
        }
    }
}