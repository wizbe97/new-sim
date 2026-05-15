using System;
using Project.Shop;
using UnityEngine;

namespace Project.UI.Phone
{
    [Serializable]
    public sealed class PhonePageItemData
    {
        [Header("Identity")]
        [SerializeField] private string itemId = "new_item";

        [Header("Display")]
        [SerializeField] private string title = "New Item";
        [SerializeField, TextArea(2, 5)] private string description = "Item description.";
        [SerializeField] private Sprite icon;

        [Header("Display Options")]
        [Tooltip("If enabled and Store Item is assigned, this button uses the StoreItemSO name, description, icon, and price.")]
        [SerializeField] private bool useStoreItemDisplayData;

        [SerializeField] private string primaryButtonText = "Open";
        [SerializeField] private string secondaryText = string.Empty;

        [Header("Action")]
        [SerializeField] private PhonePageItemActionType actionType = PhonePageItemActionType.None;

        [Tooltip("Used when Action Type is Open Page.")]
        [SerializeField] private PhonePageCatalogueSO targetPage;

        [Tooltip("Used when Action Type is Buy Store Item.")]
        [SerializeField] private StoreItemSO storeItem;

        [Header("Setting Data")]
        [Tooltip("Used when Action Type is Open Setting.")]
        [SerializeField] private string settingId = "new_setting";

        [SerializeField] private PhoneSettingDisplayType settingDisplayType = PhoneSettingDisplayType.Info;
        [SerializeField] private string settingValueText = "Default";

        [Header("State")]
        [SerializeField] private bool isDisabled;
        [SerializeField] private string disabledReason = "Unavailable";

        public string ItemId => itemId;
        public PhonePageItemActionType ActionType => actionType;
        public PhonePageCatalogueSO TargetPage => targetPage;
        public StoreItemSO StoreItem => storeItem;
        public string SettingId => settingId;
        public PhoneSettingDisplayType SettingDisplayType => settingDisplayType;
        public string SettingValueText => settingValueText;
        public bool IsDisabled => isDisabled;
        public string DisabledReason => disabledReason;

        public string GetTitle()
        {
            if (useStoreItemDisplayData && storeItem != null)
            {
                return storeItem.ItemName;
            }

            return title;
        }

        public string GetDescription()
        {
            if (useStoreItemDisplayData && storeItem != null)
            {
                return storeItem.ItemDescription;
            }

            return description;
        }

        public Sprite GetIcon()
        {
            if (useStoreItemDisplayData && storeItem != null)
            {
                return storeItem.ItemIcon;
            }

            return icon;
        }

        public string GetPrimaryButtonText()
        {
            if (isDisabled)
            {
                return disabledReason;
            }

            if (actionType == PhonePageItemActionType.BuyStoreItem && storeItem != null)
            {
                return $"Buy £{storeItem.Price:N0}";
            }

            if (actionType == PhonePageItemActionType.OpenPage)
            {
                return string.IsNullOrWhiteSpace(primaryButtonText)
                    ? "Open"
                    : primaryButtonText;
            }

            if (actionType == PhonePageItemActionType.OpenSetting)
            {
                return string.IsNullOrWhiteSpace(primaryButtonText)
                    ? "Edit"
                    : primaryButtonText;
            }

            if (actionType == PhonePageItemActionType.ClosePhone)
            {
                return string.IsNullOrWhiteSpace(primaryButtonText)
                    ? "Close"
                    : primaryButtonText;
            }

            return primaryButtonText;
        }

        public string GetSecondaryText()
        {
            if (actionType == PhonePageItemActionType.OpenSetting)
            {
                if (!string.IsNullOrWhiteSpace(settingValueText))
                {
                    return settingValueText;
                }

                return settingDisplayType.ToString();
            }

            if (actionType == PhonePageItemActionType.BuyStoreItem && storeItem != null)
            {
                return storeItem.IsUniqueItem
                    ? "Unique Item"
                    : string.Empty;
            }

            return secondaryText;
        }

#if UNITY_EDITOR
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                itemId = title;
            }

            itemId = itemId.Trim().ToLowerInvariant().Replace(" ", "_");

            if (string.IsNullOrWhiteSpace(title))
            {
                title = "New Item";
            }
        }
#endif
    }
}