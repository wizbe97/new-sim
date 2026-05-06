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
        private ItemDeliveryManager itemDeliveryManager;

        public HUDView HUD => hud;
        public BalanceDisplayView BalanceDisplay => balanceDisplay;
        public PhoneView Phone => phone;

        public event Action ShopAppPressed;
        public event Action PhoneBackPressed;
        public event Action PhoneClosePressed;

        public void Initialize(PlayerBalanceManager balanceManager, ItemDeliveryManager deliveryManager)
        {
            if (balanceManager == null)
            {
                Debug.LogError($"{nameof(UIManager)} cannot initialize because PlayerBalanceManager is missing.");
                return;
            }

            if (deliveryManager == null)
            {
                Debug.LogError($"{nameof(UIManager)} cannot initialize because ItemDeliveryManager is missing.");
                return;
            }

            playerBalanceManager = balanceManager;
            itemDeliveryManager = deliveryManager;

            CreateHUD();
            CreateBalanceDisplay();
            CreatePhone();

            SubscribeToBalanceEvents();
            RefreshBalanceDisplay();
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
                Debug.LogError($"{nameof(UIManager)} is missing HUD prefab.");
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
                Debug.LogError($"{nameof(UIManager)} is missing Balance Display prefab.");
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
                Debug.LogError($"{nameof(UIManager)} is missing Phone prefab.");
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
            if (storeItem == null)
            {
                Debug.LogError($"{nameof(UIManager)} cannot buy item because StoreItem is missing.");
                return;
            }

            if (phone != null && phone.IsUniqueItemOwned(storeItem))
            {
                Debug.Log($"{storeItem.ItemName} is unique and has already been purchased.");
                return;
            }

            if (playerBalanceManager == null)
            {
                Debug.LogError($"{nameof(UIManager)} cannot buy {storeItem.ItemName} because PlayerBalanceManager is missing.");
                return;
            }

            if (itemDeliveryManager == null)
            {
                Debug.LogError($"{nameof(UIManager)} cannot buy {storeItem.ItemName} because ItemDeliveryManager is missing.");
                return;
            }

            if (storeItem.PlaceablePrefab == null)
            {
                Debug.LogError($"{storeItem.ItemName} cannot be bought because it has no Placeable Prefab assigned.", storeItem);
                return;
            }

            if (storeItem.DeliveryBoxPrefab == null)
            {
                Debug.LogError($"{storeItem.ItemName} cannot be bought because it has no Delivery Box Prefab assigned.", storeItem);
                return;
            }

            if (!playerBalanceManager.CanAfford(storeItem.Price))
            {
                Debug.Log(
                    $"Not enough money to buy {storeItem.ItemName}. " +
                    $"Price: ${storeItem.Price:N0}, Balance: ${playerBalanceManager.CurrentBalance:N0}"
                );

                return;
            }

            bool purchaseSuccessful = playerBalanceManager.DeductBalance(storeItem.Price);

            if (!purchaseSuccessful)
            {
                Debug.Log($"Purchase failed for {storeItem.ItemName}.");
                return;
            }

            bool deliveredSuccessfully = itemDeliveryManager.DeliverItem(storeItem);

            if (!deliveredSuccessfully)
            {
                Debug.LogError(
                    $"{storeItem.ItemName} was paid for, but delivery failed. " +
                    $"You may want to refund the player here later."
                );

                return;
            }

            if (phone != null)
            {
                phone.MarkItemPurchased(storeItem);
            }

            Debug.Log($"Bought and delivered {storeItem.ItemName} for ${storeItem.Price:N0}.");
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