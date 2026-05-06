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

        [Header("Formatting")]
        [SerializeField] private string currencyPrefix = "$";
        [SerializeField] private string buyText = "Buy";
        [SerializeField] private string ownedText = "Owned";

        private StoreItemSO storeItem;
        private bool isOwned;

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
            storeItem = item;
            isOwned = owned;

            Refresh();
        }

        public void SetOwned(bool owned)
        {
            isOwned = owned;
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

            if (buyButton != null)
            {
                buyButton.interactable = !shouldDisableBecauseOwned;
            }

            if (buyButtonText != null)
            {
                buyButtonText.text = shouldDisableBecauseOwned ? ownedText : buyText;
            }
        }

        private void HandleBuyButtonClicked()
        {
            if (storeItem == null)
            {
                Debug.LogError($"{nameof(ShopItemButtonView)} on {name} cannot buy because Store Item is missing.");
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