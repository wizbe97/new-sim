using System;
using UnityEngine;

namespace Project.UI.Phone
{
    [Serializable]
    public sealed class PhoneSettingItem : PhonePageItem
    {
        [SerializeField] private string settingId = "new_setting";
        [SerializeField] private string title = "New Setting";
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private PhoneSettingDisplayType displayType = PhoneSettingDisplayType.Info;
        [SerializeField] private string valueText = "Default";
        [SerializeField] private string primaryText = "Edit";

        public override PhonePageItemActionType ActionType => PhonePageItemActionType.OpenSetting;

        public override string SettingId => settingId;
        public override PhoneSettingDisplayType SettingDisplayType => displayType;
        public override string SettingValueText => valueText;

        public override string GetTitle() => title;
        public override string GetDescription() => description;
        public override Sprite GetIcon() => icon;

        public override string GetPrimaryText()
        {
            return string.IsNullOrWhiteSpace(primaryText) ? "Edit" : primaryText;
        }

        public override string GetSecondaryText()
        {
            return !string.IsNullOrWhiteSpace(valueText)
                ? valueText
                : displayType.ToString();
        }
    }
}