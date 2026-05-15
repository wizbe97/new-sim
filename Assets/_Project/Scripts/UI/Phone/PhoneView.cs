using System;
using System.Collections.Generic;
using Project.Managers;
using Project.Shop;
using Project.UI.Phone;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class PhoneView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject phoneRoot;

        [Header("Page Rendering")]
        [SerializeField] private PhoneModularPageView modularPageView;
        [SerializeField] private PhonePageItemButtonView itemButtonPrefab;

        [Header("Start Page")]
        [SerializeField] private PhonePageCatalogueSO startingPage;

        [Header("Navigation Buttons")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button closeButton;

        public event Action BackClicked;
        public event Action CloseClicked;
        public event Action<StoreItemSO> ShopItemBuyClicked;

        private readonly Stack<PhonePageCatalogueSO> pageHistory = new();

        private UIManager owningUIManager;
        private PhonePageCatalogueSO currentPage;

        private Func<StoreItemSO, bool> ownershipProvider;
        private Func<StoreItemSO, bool> unlockProvider;

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBackClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (modularPageView != null)
            {
                modularPageView.ItemClicked += HandleItemClicked;
            }

            ShowHomePage();
        }

        private void OnDestroy()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBackClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            if (modularPageView != null)
            {
                modularPageView.ItemClicked -= HandleItemClicked;
                modularPageView.Clear();
            }
        }

        public void Initialize(UIManager uiManager)
        {
            owningUIManager = uiManager;
        }

        public void SetOwnershipProvider(Func<StoreItemSO, bool> provider)
        {
            ownershipProvider = provider;
            RefreshCurrentPage();
        }

        public void SetUnlockProvider(Func<StoreItemSO, bool> provider)
        {
            unlockProvider = provider;
            RefreshCurrentPage();
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
            pageHistory.Clear();
            OpenPage(startingPage, addCurrentPageToHistory: false);
        }

        public void ShowShopPage()
        {
            PhonePageCatalogueSO shopPage = FindPageByIdRecursive(
                startingPage,
                "shop",
                new HashSet<PhonePageCatalogueSO>());

            if (shopPage == null)
            {
                Debug.LogError($"{nameof(PhoneView)} could not find a page with id 'shop'.", this);
                return;
            }

            OpenPage(shopPage, addCurrentPageToHistory: true);
        }

        public void RefreshCurrentPage()
        {
            if (modularPageView == null)
            {
                return;
            }

            modularPageView.RefreshItems();
        }

        private void OpenPage(PhonePageCatalogueSO page, bool addCurrentPageToHistory)
        {
            if (page == null)
            {
                Debug.LogError($"{nameof(PhoneView)} on {name} cannot open page because page is missing.", this);
                return;
            }

            if (modularPageView == null)
            {
                Debug.LogError($"{nameof(PhoneView)} on {name} is missing Modular Page View.", this);
                return;
            }

            if (itemButtonPrefab == null)
            {
                Debug.LogError($"{nameof(PhoneView)} on {name} is missing Item Button Prefab.", this);
                return;
            }

            if (addCurrentPageToHistory && currentPage != null)
            {
                pageHistory.Push(currentPage);
            }

            currentPage = page;

            PhonePageContext context = CreateContext();

            modularPageView.ShowPage(
                currentPage,
                itemButtonPrefab,
                context);

            RefreshBackButton();
        }

        private PhonePageContext CreateContext()
        {
            return new PhonePageContext(
                this,
                owningUIManager,
                ownershipProvider,
                unlockProvider,
                HandleShopItemBuyRequested,
                HandleCloseClicked);
        }

        private void HandleItemClicked(PhonePageItem item)
        {
            if (item == null)
            {
                return;
            }

            switch (item.ActionType)
            {
                case PhonePageItemActionType.OpenPage:
                    HandleOpenPageItem(item);
                    break;

                case PhonePageItemActionType.BuyStoreItem:
                    HandleBuyStoreItem(item);
                    break;

                case PhonePageItemActionType.OpenSetting:
                    HandleOpenSetting(item);
                    break;

                case PhonePageItemActionType.ClosePhone:
                    HandleCloseClicked();
                    break;

                case PhonePageItemActionType.None:
                default:
                    Debug.Log($"Clicked phone item '{item.GetTitle()}', but it has no action.", this);
                    break;
            }
        }

        private void HandleOpenPageItem(PhonePageItem item)
        {
            if (item.TargetPage == null)
            {
                Debug.LogError($"Phone item '{item.GetTitle()}' cannot open page because Target Page is missing.", this);
                return;
            }

            OpenPage(item.TargetPage, addCurrentPageToHistory: true);
        }

        private void HandleBuyStoreItem(PhonePageItem item)
        {
            if (item.StoreItem == null)
            {
                Debug.LogError($"Phone item '{item.GetTitle()}' cannot buy because Store Item is missing.", this);
                return;
            }

            HandleShopItemBuyRequested(item.StoreItem);
            RefreshCurrentPage();
        }

        private void HandleOpenSetting(PhonePageItem item)
        {
            Debug.Log(
                $"Clicked setting '{item.GetTitle()}' with id '{item.SettingId}' and value '{item.SettingValueText}'.",
                this
            );
        }

        private void HandleBackClicked()
        {
            if (pageHistory.Count > 0)
            {
                PhonePageCatalogueSO previousPage = pageHistory.Pop();
                OpenPage(previousPage, addCurrentPageToHistory: false);
            }

            BackClicked?.Invoke();
            RefreshBackButton();
        }

        private void HandleCloseClicked()
        {
            CloseClicked?.Invoke();
        }

        private void HandleShopItemBuyRequested(StoreItemSO storeItem)
        {
            ShopItemBuyClicked?.Invoke(storeItem);
        }

        private void RefreshBackButton()
        {
            if (backButton == null)
            {
                return;
            }

            backButton.gameObject.SetActive(pageHistory.Count > 0);
        }

        private PhonePageCatalogueSO FindPageByIdRecursive(
            PhonePageCatalogueSO page,
            string pageId,
            HashSet<PhonePageCatalogueSO> visitedPages)
        {
            if (page == null || string.IsNullOrWhiteSpace(pageId))
            {
                return null;
            }

            if (!visitedPages.Add(page))
            {
                return null;
            }

            if (string.Equals(page.PageId, pageId.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
            {
                return page;
            }

            foreach (PhonePageItem item in page.Items)
            {
                if (item == null || item.TargetPage == null)
                {
                    continue;
                }

                PhonePageCatalogueSO foundPage = FindPageByIdRecursive(
                    item.TargetPage,
                    pageId,
                    visitedPages);

                if (foundPage != null)
                {
                    return foundPage;
                }
            }

            return null;
        }
    }
}