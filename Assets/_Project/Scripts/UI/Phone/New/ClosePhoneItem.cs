using System;
using UnityEngine;

namespace Project.UI.Phone
{
    [Serializable]
    public sealed class PhoneCloseItem : PhonePageItem
    {
        [SerializeField] private string title = "Close Phone";
        [SerializeField, TextArea(2, 5)] private string description = "Return to the game.";
        [SerializeField] private Sprite icon;
        [SerializeField] private string primaryText = "Close";

        public override PhonePageItemActionType ActionType => PhonePageItemActionType.ClosePhone;

        public override string GetTitle() => title;
        public override string GetDescription() => description;
        public override Sprite GetIcon() => icon;
        public override string GetPrimaryText() => primaryText;
    }
}