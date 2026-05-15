using System;
using Project.Shop;
using UnityEngine;

namespace Project.UI.Phone
{
    [Serializable]
    public abstract class PhonePageItem
    {
        [SerializeField] private string itemId = "new_item";
        [SerializeField] private bool isDisabled;
        [SerializeField] private string disabledReason = "Unavailable";

        public string ItemId => itemId;
        public bool IsDisabled => isDisabled;
        public string DisabledReason => disabledReason;

        public abstract PhonePageItemActionType ActionType { get; }

        public virtual string GetTitle() => itemId;
        public virtual string GetDescription() => string.Empty;
        public virtual Sprite GetIcon() => null;
        public virtual string GetPrimaryText() => "Open";
        public virtual string GetSecondaryText() => string.Empty;

        public virtual PhonePageCatalogueSO TargetPage => null;
        public virtual StoreItemSO StoreItem => null;

        public virtual string SettingId => string.Empty;
        public virtual PhoneSettingDisplayType SettingDisplayType => PhoneSettingDisplayType.Info;
        public virtual string SettingValueText => string.Empty;

#if UNITY_EDITOR
        public virtual void Validate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                itemId = GetTitle();
            }

            itemId = itemId.Trim().ToLowerInvariant().Replace(" ", "_");
        }
#endif
    }
}