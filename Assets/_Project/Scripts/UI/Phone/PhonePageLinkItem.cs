using System;
using UnityEngine;

namespace Project.UI.Phone
{
    [Serializable]
    public sealed class PhonePageLinkItem : PhonePageItem
    {
        [SerializeField] private PhonePageCatalogueSO targetPage;
        [SerializeField] private string titleOverride;
        [SerializeField, TextArea(2, 5)] private string descriptionOverride;
        [SerializeField] private Sprite iconOverride;
        [SerializeField] private string primaryText = "Open";

        public override PhonePageItemActionType ActionType => PhonePageItemActionType.OpenPage;
        public override PhonePageCatalogueSO TargetPage => targetPage;

        public override string GetTitle()
        {
            if (!string.IsNullOrWhiteSpace(titleOverride))
            {
                return titleOverride;
            }

            return targetPage != null ? targetPage.PageTitle : "Missing Page";
        }

        public override string GetDescription()
        {
            if (!string.IsNullOrWhiteSpace(descriptionOverride))
            {
                return descriptionOverride;
            }

            return targetPage != null ? targetPage.PageDescription : string.Empty;
        }

        public override Sprite GetIcon()
        {
            return iconOverride != null
                ? iconOverride
                : targetPage != null ? targetPage.PageIcon : null;
        }

        public override string GetPrimaryText()
        {
            return string.IsNullOrWhiteSpace(primaryText) ? "Open" : primaryText;
        }
    }
}