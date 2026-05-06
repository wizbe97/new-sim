using System;
using Project.Shop;
using Project.UI;
using UnityEngine;

namespace Project.Managers
{
    public sealed class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private HUDView hudPrefab;

        [Header("Balance Display")]
        [SerializeField] private BalanceDisplayView balanceDisplayPrefab;

        [Header("Phone")]
        [SerializeField] private PhoneView phonePrefab;

        private HUDView hud;
        private BalanceDisplayView balanceDisplay;
        private PhoneView phone;

        private PlayerBalanceManager playerBalanceManager;

        public HUDView HUD => hud;
        public BalanceDisplayView BalanceDisplay => balanceDisplay;
        public PhoneView Phone => phone;

        public event Action ShopAppPressed;
        public event Action PhoneBackPressed;
        public event Action PhoneClosePressed;
        public event Action<StoreItemSO> ShopItemBuyRequested;

        public void Initialize(PlayerBalanceManager balanceManager)
        {
            if (balanceManager == null)
            {
                Debug.LogError($"{nameof(UIManager)} cannot initialize because PlayerBalanceManager is missing.", this);
                return;
            }

            playerBalanceManager = balanceManager;

            CreateHUD();
            CreateBalanceDisplay();
            CreatePhone();

            SubscribeToBalanceEvents();
            RefreshBalanceDisplay();
        }

        public void SetShopOwnershipProvider(Func<StoreItemSO, bool> ownershipProvider)
        {
            if (phone == null)
            {
                return;
            }

            phone.SetOwnershipProvider(ownershipProvider);
        }

        public void RefreshPhoneShopItems()
        {
            if (phone == null)
            {
                return;
            }

            phone.RefreshShopItems();
        }

        public void SetReticleVisible(bool isVisible)
        {
            if (hud != null)
            {
                hud.SetReticleVisible(isVisible);
            }

            if (balanceDisplay != null)
            {
                balanceDisplay.gameObject.SetActive(true);
            }
        }

        public void SetGameplayHUDVisible(bool isVisible)
        {
            SetReticleVisible(isVisible);
        }

        public void SetReticleInteractableState(bool hasInteractable)
        {
            if (hud == null)
            {
                return;
            }

            hud.SetReticleInteractableState(hasInteractable);
        }

        public void SetBalance(int balance)
        {
            if (balanceDisplay == null)
            {
                return;
            }

            balanceDisplay.SetBalance(balance);
        }

        public void ShowPhone()
        {
            if (phone == null)
            {
                return;
            }

            phone.Show();
        }

        public void HidePhone()
        {
            if (phone == null)
            {
                return;
            }

            phone.Hide();
        }

        public void ShowPhoneHomePage()
        {
            if (phone == null)
            {
                return;
            }

            phone.ShowHomePage();
        }

        public void ShowPhoneShopPage()
        {
            if (phone == null)
            {
                return;
            }

            phone.ShowShopPage();
        }

        private void CreateHUD()
        {
            if (hudPrefab == null)
            {
                Debug.LogError($"{nameof(UIManager)} is missing HUD prefab.", this);
                return;
            }

            hud = Instantiate(hudPrefab);
            hud.name = "HUDCanvas";

            hud.ShowReticle();
            hud.SetReticleInteractableState(false);
        }

        private void CreateBalanceDisplay()
        {
            if (balanceDisplayPrefab == null)
            {
                Debug.LogError($"{nameof(UIManager)} is missing Balance Display prefab.", this);
                return;
            }

            balanceDisplay = Instantiate(balanceDisplayPrefab);
            balanceDisplay.name = "BalanceDisplayCanvas";
            balanceDisplay.SetBalance(0);
        }

        private void CreatePhone()
        {
            if (phonePrefab == null)
            {
                Debug.LogError($"{nameof(UIManager)} is missing Phone prefab.", this);
                return;
            }

            phone = Instantiate(phonePrefab);
            phone.name = "PhoneCanvas";

            phone.ShopAppClicked += HandleShopAppClicked;
            phone.BackClicked += HandlePhoneBackClicked;
            phone.CloseClicked += HandlePhoneCloseClicked;
            phone.ShopItemBuyClicked += HandleShopItemBuyClicked;

            phone.ShowHomePage();
            phone.Hide();
        }

        private void SubscribeToBalanceEvents()
        {
            playerBalanceManager.BalanceChanged += HandleBalanceChanged;
        }

        private void RefreshBalanceDisplay()
        {
            SetBalance(playerBalanceManager.CurrentBalance);
        }

        private void HandleBalanceChanged(int newBalance)
        {
            SetBalance(newBalance);
        }

        private void HandleShopAppClicked()
        {
            ShopAppPressed?.Invoke();
        }

        private void HandlePhoneBackClicked()
        {
            PhoneBackPressed?.Invoke();
        }

        private void HandlePhoneCloseClicked()
        {
            PhoneClosePressed?.Invoke();
        }

        private void HandleShopItemBuyClicked(StoreItemSO storeItem)
        {
            ShopItemBuyRequested?.Invoke(storeItem);
        }

        private void OnDestroy()
        {
            if (playerBalanceManager != null)
            {
                playerBalanceManager.BalanceChanged -= HandleBalanceChanged;
            }

            if (phone != null)
            {
                phone.ShopAppClicked -= HandleShopAppClicked;
                phone.BackClicked -= HandlePhoneBackClicked;
                phone.CloseClicked -= HandlePhoneCloseClicked;
                phone.ShopItemBuyClicked -= HandleShopItemBuyClicked;
            }
        }
    }
}