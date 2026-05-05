using System;
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

        [Header("Shop")]
        [SerializeField] private ShopItemButtonView testShopItemButton;

        public event Action ShopAppClicked;
        public event Action BackClicked;
        public event Action CloseClicked;
        public event Action<StoreItemSO> ShopItemBuyClicked;

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

            if (testShopItemButton != null)
            {
                testShopItemButton.BuyPressed += HandleShopItemBuyPressed;
                testShopItemButton.Refresh();
            }
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

            if (testShopItemButton != null)
            {
                testShopItemButton.BuyPressed -= HandleShopItemBuyPressed;
            }
        }

        public void Show()
        {
            if (phoneRoot == null)
            {
                Debug.LogError($"{nameof(PhoneView)} on {name} is missing Phone Root.");
                return;
            }

            phoneRoot.SetActive(true);
        }

        public void Hide()
        {
            if (phoneRoot == null)
            {
                Debug.LogError($"{nameof(PhoneView)} on {name} is missing Phone Root.");
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
        }

        private void SetPageVisibility(bool showHomePage)
        {
            if (homePageRoot == null)
            {
                Debug.LogError($"{nameof(PhoneView)} on {name} is missing Home Page Root.");
                return;
            }

            if (shopPageRoot == null)
            {
                Debug.LogError($"{nameof(PhoneView)} on {name} is missing Shop Page Root.");
                return;
            }

            homePageRoot.SetActive(showHomePage);
            shopPageRoot.SetActive(!showHomePage);
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