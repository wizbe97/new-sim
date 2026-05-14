using System;
using System.Collections.Generic;
using Project.Managers;
using Project.Progression;
using UnityEngine;

namespace Project.Shop
{
    public sealed class ShopPurchaseService : MonoBehaviour
    {
        private PlayerBalanceManager balanceManager;
        private ItemDeliveryManager deliveryManager;
        private CasinoProgressionManager casinoProgressionManager;

        private readonly HashSet<string> purchasedUniqueItemIds = new();

        public event Action<StoreItemSO> ItemPurchased;

        public void Initialize(
            PlayerBalanceManager newBalanceManager,
            ItemDeliveryManager newDeliveryManager,
            CasinoProgressionManager newCasinoProgressionManager)
        {
            if (newBalanceManager == null)
            {
                Debug.LogError($"{nameof(ShopPurchaseService)} cannot initialize because PlayerBalanceManager is missing.", this);
                return;
            }

            if (newDeliveryManager == null)
            {
                Debug.LogError($"{nameof(ShopPurchaseService)} cannot initialize because ItemDeliveryManager is missing.", this);
                return;
            }

            if (newCasinoProgressionManager == null)
            {
                Debug.LogError($"{nameof(ShopPurchaseService)} cannot initialize because CasinoProgressionManager is missing.", this);
                return;
            }

            balanceManager = newBalanceManager;
            deliveryManager = newDeliveryManager;
            casinoProgressionManager = newCasinoProgressionManager;
        }

        public bool TryBuyItem(StoreItemSO storeItem)
        {
            if (!CanAttemptPurchase(storeItem))
            {
                return false;
            }

            if (!IsItemUnlocked(storeItem))
            {
                Debug.Log(
                    $"{storeItem.ItemName} requires casino level {storeItem.RequiredCasinoLevel}. " +
                    $"Current casino level: {casinoProgressionManager.CurrentLevel}.",
                    storeItem
                );

                return false;
            }

            if (IsUniqueItemOwned(storeItem))
            {
                Debug.Log($"{storeItem.ItemName} is unique and has already been purchased.", storeItem);
                return false;
            }

            if (!balanceManager.CanAfford(storeItem.Price))
            {
                Debug.Log(
                    $"Not enough money to buy {storeItem.ItemName}. " +
                    $"Price: ${storeItem.Price:N0}, Balance: ${balanceManager.CurrentBalance:N0}",
                    storeItem
                );

                return false;
            }

            if (!balanceManager.TrySpend(storeItem.Price))
            {
                Debug.Log($"Purchase failed for {storeItem.ItemName}.", storeItem);
                return false;
            }

            bool delivered = deliveryManager.DeliverItem(storeItem);

            if (!delivered)
            {
                balanceManager.AddBalance(storeItem.Price);

                Debug.LogError(
                    $"{storeItem.ItemName} purchase was refunded because delivery failed.",
                    storeItem
                );

                return false;
            }

            MarkPurchased(storeItem);

            casinoProgressionManager.AddConfiguredXp(CasinoXpSource.ItemPurchased);

            Debug.Log($"Bought and delivered {storeItem.ItemName} for ${storeItem.Price:N0}.", storeItem);

            ItemPurchased?.Invoke(storeItem);
            return true;
        }

        public bool IsUniqueItemOwned(StoreItemSO storeItem)
        {
            return storeItem != null &&
                   storeItem.IsUniqueItem &&
                   purchasedUniqueItemIds.Contains(storeItem.ItemId);
        }

        public bool IsItemUnlocked(StoreItemSO storeItem)
        {
            if (storeItem == null)
            {
                return false;
            }

            if (casinoProgressionManager == null)
            {
                return false;
            }

            return casinoProgressionManager.CanUseLevel(storeItem.RequiredCasinoLevel);
        }

        private bool CanAttemptPurchase(StoreItemSO storeItem)
        {
            if (storeItem == null)
            {
                Debug.LogError($"{nameof(ShopPurchaseService)} cannot buy item because StoreItemSO is missing.", this);
                return false;
            }

            if (balanceManager == null)
            {
                Debug.LogError($"{nameof(ShopPurchaseService)} cannot buy {storeItem.ItemName} because PlayerBalanceManager is missing.", this);
                return false;
            }

            if (deliveryManager == null)
            {
                Debug.LogError($"{nameof(ShopPurchaseService)} cannot buy {storeItem.ItemName} because ItemDeliveryManager is missing.", this);
                return false;
            }

            if (casinoProgressionManager == null)
            {
                Debug.LogError($"{nameof(ShopPurchaseService)} cannot buy {storeItem.ItemName} because CasinoProgressionManager is missing.", this);
                return false;
            }

            if (storeItem.PlaceablePrefab == null)
            {
                Debug.LogError($"{storeItem.ItemName} cannot be bought because it has no Placeable Prefab assigned.", storeItem);
                return false;
            }

            if (storeItem.DeliveryBoxPrefab == null)
            {
                Debug.LogError($"{storeItem.ItemName} cannot be bought because it has no Delivery Box Prefab assigned.", storeItem);
                return false;
            }

            return true;
        }

        private void MarkPurchased(StoreItemSO storeItem)
        {
            if (storeItem == null || !storeItem.IsUniqueItem)
            {
                return;
            }

            purchasedUniqueItemIds.Add(storeItem.ItemId);
        }
    }
}