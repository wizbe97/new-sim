using System;
using System.Collections.Generic;
using Project.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class PhoneView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject phoneRoot;

        [Header("Pages")]
        [SerializeField] private GameObject homePageRoot;
        [SerializeField] private GameObject shopPageRoot;

        [Header("Buttons")]
        [SerializeField] private Button shopAppButton;
        [SerializeField] private Button shopBackButton;
        [SerializeField] private Button closeButton;

        [Header("Shop Catalogue")]
        [SerializeField] private ShopCatalogueSO shopCatalogue;

        [Header("Shop UI")]
        [SerializeField] private ShopItemButtonView shopItemButtonPrefab;
        [SerializeField] private RectTransform shopItemsContentRoot;

        public event Action ShopAppClicked;
        public event Action BackClicked;
        public event Action CloseClicked;
        public event Action<StoreItemSO> ShopItemBuyClicked;

        private readonly List<ShopItemButtonView> spawnedShopItems = new();

        private Func<StoreItemSO, bool> ownershipProvider;

        private void Awake()
        {
            if (shopAppButton != null)
            {
                shopAppButton.onClick.AddListener(HandleShopAppClicked);
            }

            if (shopBackButton != null)
            {
                shopBackButton.onClick.AddListener(HandleBackClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleCloseClicked);
            }

            PopulateShop();
        }

        private void OnDestroy()
        {
            if (shopAppButton != null)
            {
                shopAppButton.onClick.RemoveListener(HandleShopAppClicked);
            }

            if (shopBackButton != null)
            {
                shopBackButton.onClick.RemoveListener(HandleBackClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            ClearShopItems();
        }

        public void SetOwnershipProvider(Func<StoreItemSO, bool> provider)
        {
            ownershipProvider = provider;
            RefreshShopItems();
        }

        public void Show()
        {
            if (phoneRoot == null)
            {
                Debug.LogError($"{nameof(PhoneView)} on {name} is missing Phone Root.", this);
                return;
            }

            phoneRoot.SetActive(true);
        }

        public void Hide()
        {
            if (phoneRoot == null)
            {
                Debug.LogError($"{nameof(PhoneView)} on {name} is missing Phone Root.", this);
                return;
            }

            phoneRoot.SetActive(false);
        }

        public void ShowHomePage()
        {
            SetPageVisibility(showHomePage: true);
        }

        public void ShowShopPage()
        {
            SetPageVisibility(showHomePage: false);
            RefreshShopItems();
        }

        public void RefreshShopItems()
        {
            foreach (ShopItemButtonView itemView in spawnedShopItems)
            {
                if (itemView == null || itemView.StoreItem == null)
                {
                    continue;
                }

                itemView.SetOwned(IsOwned(itemView.StoreItem));
            }
        }

        public void RebuildShop()
        {
            PopulateShop();
        }

        private void SetPageVisibility(bool showHomePage)
        {
            if (homePageRoot == null)
            {
                Debug.LogError($"{nameof(PhoneView)} on {name} is missing Home Page Root.", this);
                return;
            }

            if (shopPageRoot == null)
            {
                Debug.LogError($"{nameof(PhoneView)} on {name} is missing Shop Page Root.", this);
                return;
            }

            homePageRoot.SetActive(showHomePage);
            shopPageRoot.SetActive(!showHomePage);
        }

        private void PopulateShop()
        {
            ClearShopItems();

            if (shopCatalogue == null)
            {
                Debug.LogError($"{nameof(PhoneView)} on {name} is missing Shop Catalogue.", this);
                return;
            }

            if (shopItemButtonPrefab == null)
            {
                Debug.LogError($"{nameof(PhoneView)} on {name} is missing Shop Item Button Prefab.", this);
                return;
            }

            if (shopItemsContentRoot == null)
            {
                Debug.LogError($"{nameof(PhoneView)} on {name} is missing Shop Items Content Root.", this);
                return;
            }

            foreach (StoreItemSO item in shopCatalogue.Items)
            {
                if (item == null)
                {
                    continue;
                }

                ShopItemButtonView itemView = Instantiate(shopItemButtonPrefab, shopItemsContentRoot);
                itemView.name = $"ShopItemButton_{item.ItemName}";
                itemView.Initialize(item, IsOwned(item));
                itemView.BuyPressed += HandleShopItemBuyPressed;

                spawnedShopItems.Add(itemView);
            }
        }

        private void ClearShopItems()
        {
            for (int i = 0; i < spawnedShopItems.Count; i++)
            {
                ShopItemButtonView itemView = spawnedShopItems[i];

                if (itemView == null)
                {
                    continue;
                }

                itemView.BuyPressed -= HandleShopItemBuyPressed;
                Destroy(itemView.gameObject);
            }

            spawnedShopItems.Clear();
        }

        private bool IsOwned(StoreItemSO storeItem)
        {
            return ownershipProvider != null && ownershipProvider.Invoke(storeItem);
        }

        private void HandleShopAppClicked()
        {
            ShopAppClicked?.Invoke();
        }

        private void HandleBackClicked()
        {
            BackClicked?.Invoke();
        }

        private void HandleCloseClicked()
        {
            CloseClicked?.Invoke();
        }

        private void HandleShopItemBuyPressed(StoreItemSO storeItem)
        {
            ShopItemBuyClicked?.Invoke(storeItem);
        }
    }
}