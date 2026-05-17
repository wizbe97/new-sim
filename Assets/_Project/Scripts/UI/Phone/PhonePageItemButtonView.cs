using System;
using Project.Shop;
using Project.Staff;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI.Phone
{
    public sealed class PhonePageItemButtonView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI primaryText;
        [SerializeField] private TextMeshProUGUI secondaryText;

        [Header("Colours")]
        [SerializeField] private Color normalTextColour = Color.white;
        [SerializeField] private Color disabledTextColour = Color.gray;
        [SerializeField] private Color lockedTextColour = new Color(1f, 0.65f, 0.1f, 1f);
        [SerializeField] private Color ownedTextColour = new Color(0.4f, 1f, 0.4f, 1f);

        private PhonePageItem item;
        private PhonePageContext context;
        private PhonePageLayoutMode layoutMode;

        private PhoneVerticalListLayoutSettings listSettings;
        private PhoneIconGridLayoutSettings gridSettings;

        private int effectiveListColumnCount = 1;
        private bool showDetailedDescriptions = true;

        private RectTransform rectTransform;
        private LayoutElement layoutElement;

        public event Action<PhonePageItem> Clicked;

        public PhonePageItem Item => item;
        public PhoneVerticalListLayoutSettings ListSettings => listSettings;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            layoutElement = GetComponent<LayoutElement>();

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        public void Initialize(
            PhonePageItem newItem,
            PhonePageContext newContext,
            PhonePageLayoutMode newLayoutMode,
            PhoneVerticalListLayoutSettings newListSettings,
            PhoneIconGridLayoutSettings newGridSettings,
            int newEffectiveListColumnCount,
            bool newShowDetailedDescriptions)
        {
            item = newItem;
            context = newContext;
            layoutMode = newLayoutMode;
            listSettings = newListSettings;
            gridSettings = newGridSettings;
            effectiveListColumnCount = Mathf.Max(1, newEffectiveListColumnCount);
            showDetailedDescriptions = newShowDetailedDescriptions;

            ConfigureVisualLayout();
            Refresh();
        }

        public void Refresh()
        {
            if (item == null)
            {
                Debug.LogError($"{nameof(PhonePageItemButtonView)} on {name} cannot refresh because item is missing.", this);
                return;
            }

            Sprite icon = item.GetIcon();

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
            }

            if (titleText != null)
            {
                titleText.text = item.GetTitle();
            }

            if (descriptionText != null)
            {
                descriptionText.text = item.GetDescription();
            }

            ApplyState();
        }

        public void Dispose()
        {
            Clicked = null;
            item = null;
            context = null;
            listSettings = null;
            gridSettings = null;
            effectiveListColumnCount = 1;
            showDetailedDescriptions = true;
        }

        private void ConfigureVisualLayout()
        {
            if (layoutElement == null)
            {
                layoutElement = GetComponent<LayoutElement>();
            }

            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (layoutMode == PhonePageLayoutMode.IconGrid)
            {
                ConfigureGridVisualLayout();
            }
            else
            {
                ConfigureListVisualLayout();
            }
        }

        private void ConfigureGridVisualLayout()
        {
            if (gridSettings == null)
            {
                Debug.LogError($"{nameof(PhonePageItemButtonView)} on {name} is missing grid settings.", this);
                return;
            }

            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = false;
                layoutElement.preferredWidth = -1f;
                layoutElement.preferredHeight = -1f;
                layoutElement.flexibleWidth = 0f;
                layoutElement.flexibleHeight = 0f;
            }

            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(true);

                RectTransform iconRect = iconImage.rectTransform;
                iconRect.anchorMin = new Vector2(0.5f, 1f);
                iconRect.anchorMax = new Vector2(0.5f, 1f);
                iconRect.pivot = new Vector2(0.5f, 1f);
                iconRect.anchoredPosition = gridSettings.IconAnchoredPosition;
                iconRect.sizeDelta = gridSettings.IconSize;
            }

            if (titleText != null)
            {
                titleText.gameObject.SetActive(true);
                titleText.fontSize = gridSettings.TitleFontSize;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.textWrappingMode = TextWrappingModes.NoWrap;
                titleText.overflowMode = TextOverflowModes.Ellipsis;

                RectTransform titleRect = titleText.rectTransform;
                titleRect.anchorMin = new Vector2(0f, 0f);
                titleRect.anchorMax = new Vector2(1f, 0f);
                titleRect.pivot = new Vector2(0.5f, 0f);
                titleRect.offsetMin = gridSettings.TitleOffsetMin;
                titleRect.offsetMax = gridSettings.TitleOffsetMax;
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(false);
            }

            if (primaryText != null)
            {
                primaryText.gameObject.SetActive(false);
            }

            if (secondaryText != null)
            {
                secondaryText.gameObject.SetActive(false);
            }
        }

        private void ConfigureListVisualLayout()
        {
            if (listSettings == null)
            {
                Debug.LogError($"{nameof(PhonePageItemButtonView)} on {name} is missing list settings.", this);
                return;
            }

            bool isCompactColumnView = effectiveListColumnCount > 1 && !showDetailedDescriptions;

            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = false;
                layoutElement.preferredWidth = -1f;
                layoutElement.preferredHeight = listSettings.ItemHeight;
                layoutElement.flexibleWidth = effectiveListColumnCount > 1 ? 0f : 1f;
                layoutElement.flexibleHeight = 0f;
            }

            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
            }

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(true);

                RectTransform iconRect = iconImage.rectTransform;
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = listSettings.IconAnchoredPosition;
                iconRect.sizeDelta = listSettings.IconSize;
            }

            if (titleText != null)
            {
                titleText.gameObject.SetActive(true);
                titleText.fontSize = listSettings.TitleFontSize;
                titleText.alignment = TextAlignmentOptions.Left;
                titleText.fontStyle = FontStyles.Bold;
                titleText.textWrappingMode = isCompactColumnView
                    ? TextWrappingModes.Normal
                    : TextWrappingModes.NoWrap;
                titleText.overflowMode = isCompactColumnView
                    ? TextOverflowModes.Overflow
                    : TextOverflowModes.Ellipsis;

                RectTransform titleRect = titleText.rectTransform;

                if (isCompactColumnView)
                {
                    titleRect.anchorMin = new Vector2(0f, 0f);
                    titleRect.anchorMax = new Vector2(1f, 1f);
                    titleRect.pivot = new Vector2(0f, 0.5f);
                    titleRect.offsetMin = new Vector2(listSettings.TitleOffsetMin.x, 8f);
                    titleRect.offsetMax = new Vector2(listSettings.TitleOffsetMax.x, -8f);
                }
                else
                {
                    titleRect.anchorMin = new Vector2(0f, 1f);
                    titleRect.anchorMax = new Vector2(1f, 1f);
                    titleRect.pivot = new Vector2(0f, 1f);
                    titleRect.offsetMin = listSettings.TitleOffsetMin;
                    titleRect.offsetMax = listSettings.TitleOffsetMax;
                }
            }

            if (descriptionText != null)
            {
                bool showDescription = showDetailedDescriptions && effectiveListColumnCount <= 1;

                descriptionText.gameObject.SetActive(showDescription);

                if (showDescription)
                {
                    descriptionText.fontSize = listSettings.DescriptionFontSize;
                    descriptionText.alignment = TextAlignmentOptions.TopLeft;
                    descriptionText.fontStyle = FontStyles.Normal;
                    descriptionText.textWrappingMode = TextWrappingModes.Normal;
                    descriptionText.overflowMode = TextOverflowModes.Ellipsis;

                    RectTransform descriptionRect = descriptionText.rectTransform;
                    descriptionRect.anchorMin = new Vector2(0f, 0f);
                    descriptionRect.anchorMax = new Vector2(1f, 1f);
                    descriptionRect.pivot = new Vector2(0f, 0.5f);
                    descriptionRect.offsetMin = listSettings.DescriptionOffsetMin;
                    descriptionRect.offsetMax = listSettings.DescriptionOffsetMax;
                }
            }

            if (primaryText != null)
            {
                primaryText.gameObject.SetActive(true);
                primaryText.fontSize = listSettings.PrimaryFontSize;
                primaryText.alignment = TextAlignmentOptions.Right;
                primaryText.textWrappingMode = TextWrappingModes.NoWrap;
                primaryText.overflowMode = TextOverflowModes.Ellipsis;

                RectTransform primaryRect = primaryText.rectTransform;
                primaryRect.anchorMin = new Vector2(1f, 0.5f);
                primaryRect.anchorMax = new Vector2(1f, 0.5f);
                primaryRect.pivot = new Vector2(1f, 0.5f);
                primaryRect.anchoredPosition = listSettings.PrimaryAnchoredPosition;
                primaryRect.sizeDelta = listSettings.PrimarySize;
            }

            if (secondaryText != null)
            {
                secondaryText.gameObject.SetActive(true);
                secondaryText.fontSize = listSettings.SecondaryFontSize;
                secondaryText.alignment = TextAlignmentOptions.Right;
                secondaryText.textWrappingMode = TextWrappingModes.NoWrap;
                secondaryText.overflowMode = TextOverflowModes.Ellipsis;

                RectTransform secondaryRect = secondaryText.rectTransform;
                secondaryRect.anchorMin = new Vector2(1f, 0.5f);
                secondaryRect.anchorMax = new Vector2(1f, 0.5f);
                secondaryRect.pivot = new Vector2(1f, 0.5f);
                secondaryRect.anchoredPosition = listSettings.SecondaryAnchoredPosition;
                secondaryRect.sizeDelta = listSettings.SecondarySize;
            }
        }

        private void ApplyState()
        {
            bool canClick = true;
            string primary = item.GetPrimaryText();
            string secondary = item.GetSecondaryText();
            Color textColour = normalTextColour;

            if (item.IsDisabled)
            {
                canClick = false;
                textColour = disabledTextColour;
                primary = item.DisabledReason;
            }
            else if (item.ActionType == PhonePageItemActionType.BuyStoreItem)
            {
                ApplyStoreItemState(ref canClick, ref primary, ref secondary, ref textColour);
            }
            else if (item.ActionType == PhonePageItemActionType.HireStaffMember)
            {
                ApplyStaffMemberState(ref canClick, ref primary, ref secondary, ref textColour);
            }
            else if (item.ActionType == PhonePageItemActionType.None)
            {
                canClick = false;
            }

            if (button != null)
            {
                button.interactable = canClick;
            }

            if (titleText != null)
            {
                titleText.color = textColour;
            }

            if (descriptionText != null)
            {
                descriptionText.color = textColour;
            }

            if (primaryText != null)
            {
                primaryText.text = primary;
                primaryText.color = textColour;
            }

            if (secondaryText != null)
            {
                secondaryText.text = secondary;
                secondaryText.color = textColour;
                secondaryText.gameObject.SetActive(
                    layoutMode == PhonePageLayoutMode.VerticalList &&
                    !string.IsNullOrWhiteSpace(secondary));
            }

            if (descriptionText != null)
            {
                bool showDescription =
                    layoutMode == PhonePageLayoutMode.VerticalList &&
                    showDetailedDescriptions &&
                    effectiveListColumnCount <= 1;

                descriptionText.gameObject.SetActive(showDescription);
            }
        }

        private void ApplyStoreItemState(
            ref bool canClick,
            ref string primary,
            ref string secondary,
            ref Color textColour)
        {
            StoreItemSO storeItem = item.StoreItem;

            if (storeItem == null)
            {
                canClick = false;
                primary = "Missing Item";
                textColour = disabledTextColour;
                return;
            }

            bool isUnlocked = context == null || context.IsShopItemUnlocked(storeItem);
            bool isOwned = context != null && context.IsShopItemOwned(storeItem);

            if (!isUnlocked)
            {
                canClick = false;
                primary = $"Requires Level {storeItem.RequiredCasinoLevel}";
                textColour = lockedTextColour;
                secondary = "Locked";
                return;
            }

            if (storeItem.IsUniqueItem && isOwned)
            {
                canClick = false;
                primary = "Owned";
                textColour = ownedTextColour;
                secondary = "Purchased";
                return;
            }

            canClick = true;
            primary = $"Buy £{storeItem.Price:N0}";
        }

        private void ApplyStaffMemberState(
            ref bool canClick,
            ref string primary,
            ref string secondary,
            ref Color textColour)
        {
            StaffMemberSO staffMember = item.StaffMember;

            if (staffMember == null)
            {
                canClick = false;
                primary = "Missing Staff";
                textColour = disabledTextColour;
                return;
            }

            if (staffMember.Prefab == null)
            {
                canClick = false;
                primary = "Missing Prefab";
                textColour = disabledTextColour;
                secondary = "Invalid";
                return;
            }

            bool isUnlocked = context == null || context.IsStaffMemberUnlocked(staffMember);
            bool isHired = context != null && context.IsStaffMemberHired(staffMember);

            if (!isUnlocked)
            {
                canClick = false;
                primary = $"Requires Level {staffMember.RequiredCasinoLevel}";
                textColour = lockedTextColour;
                secondary = "Locked";
                return;
            }

            if (staffMember.IsUniqueHire && isHired)
            {
                canClick = false;
                primary = "Hired";
                textColour = ownedTextColour;
                secondary = "Employed";
                return;
            }

            canClick = true;
            primary = $"Hire £{staffMember.UnlockCost:N0}";
        }

        private void HandleClicked()
        {
            if (item == null)
            {
                Debug.LogError($"{nameof(PhonePageItemButtonView)} on {name} cannot click because item is missing.", this);
                return;
            }

            Clicked?.Invoke(item);
        }
    }
}