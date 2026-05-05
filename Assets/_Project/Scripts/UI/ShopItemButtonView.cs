using System;
using Project.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class ShopItemButtonView : MonoBehaviour
    {
        [Header("Item")]
        [SerializeField] private StoreItemSO storeItem;

        [Header("References")]
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button buyButton;

        [Header("Formatting")]
        [SerializeField] private string currencyPrefix = "$";

        public event Action<StoreItemSO> BuyPressed;

        public StoreItemSO StoreItem => storeItem;

        private void Awake()
        {
            if (buyButton != null)
            {
                buyButton.onClick.AddListener(HandleBuyButtonClicked);
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(HandleBuyButtonClicked);
            }
        }

        public void Refresh()
        {
            if (storeItem == null)
            {
                Debug.LogError($"{nameof(ShopItemButtonView)} on {name} is missing Store Item.");
                return;
            }

            if (itemNameText != null)
            {
                itemNameText.text = storeItem.DisplayName;
            }

            if (priceText != null)
            {
                priceText.text = $"{currencyPrefix}{storeItem.Price:N0}";
            }
        }

        private void HandleBuyButtonClicked()
        {
            if (storeItem == null)
            {
                Debug.LogError($"{nameof(ShopItemButtonView)} on {name} cannot buy because Store Item is missing.");
                return;
            }

            BuyPressed?.Invoke(storeItem);
        }
    }
}