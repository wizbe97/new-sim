using System;
using Project.Shop;
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

        private RectTransform rectTransform;
        private LayoutElement layoutElement;

        public event Action<PhonePageItem> Clicked;

        public PhonePageItem Item => item;

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
            PhonePageLayoutMode newLayoutMode)
        {
            item = newItem;
            context = newContext;
            layoutMode = newLayoutMode;

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
                iconRect.anchoredPosition = new Vector2(0f, -10f);
                iconRect.sizeDelta = new Vector2(54f, 54f);
            }

            if (titleText != null)
            {
                titleText.gameObject.SetActive(true);
                titleText.fontSize = 13f;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.enableWordWrapping = false;
                titleText.overflowMode = TextOverflowModes.Ellipsis;

                RectTransform titleRect = titleText.rectTransform;
                titleRect.anchorMin = new Vector2(0f, 0f);
                titleRect.anchorMax = new Vector2(1f, 0f);
                titleRect.pivot = new Vector2(0.5f, 0f);
                titleRect.offsetMin = new Vector2(6f, 8f);
                titleRect.offsetMax = new Vector2(-6f, 44f);
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
            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = false;
                layoutElement.preferredWidth = -1f;
                layoutElement.preferredHeight = 96f;
                layoutElement.flexibleWidth = 1f;
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
                iconRect.anchoredPosition = new Vector2(14f, 0f);
                iconRect.sizeDelta = new Vector2(54f, 54f);
            }

            if (titleText != null)
            {
                titleText.gameObject.SetActive(true);
                titleText.fontSize = 20f;
                titleText.alignment = TextAlignmentOptions.Left;
                titleText.enableWordWrapping = false;
                titleText.overflowMode = TextOverflowModes.Ellipsis;

                RectTransform titleRect = titleText.rectTransform;
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.pivot = new Vector2(0f, 1f);
                titleRect.offsetMin = new Vector2(82f, -36f);
                titleRect.offsetMax = new Vector2(-110f, -8f);
            }

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.fontSize = 13f;
                descriptionText.alignment = TextAlignmentOptions.TopLeft;
                descriptionText.enableWordWrapping = true;
                descriptionText.overflowMode = TextOverflowModes.Ellipsis;

                RectTransform descriptionRect = descriptionText.rectTransform;
                descriptionRect.anchorMin = new Vector2(0f, 0f);
                descriptionRect.anchorMax = new Vector2(1f, 1f);
                descriptionRect.pivot = new Vector2(0f, 0.5f);
                descriptionRect.offsetMin = new Vector2(82f, 10f);
                descriptionRect.offsetMax = new Vector2(-110f, -38f);
            }

            if (primaryText != null)
            {
                primaryText.gameObject.SetActive(true);
                primaryText.fontSize = 15f;
                primaryText.alignment = TextAlignmentOptions.Right;
                primaryText.enableWordWrapping = false;
                primaryText.overflowMode = TextOverflowModes.Ellipsis;

                RectTransform primaryRect = primaryText.rectTransform;
                primaryRect.anchorMin = new Vector2(1f, 0.5f);
                primaryRect.anchorMax = new Vector2(1f, 0.5f);
                primaryRect.pivot = new Vector2(1f, 0.5f);
                primaryRect.anchoredPosition = new Vector2(-10f, 12f);
                primaryRect.sizeDelta = new Vector2(95f, 28f);
            }

            if (secondaryText != null)
            {
                secondaryText.gameObject.SetActive(true);
                secondaryText.fontSize = 12f;
                secondaryText.alignment = TextAlignmentOptions.Right;
                secondaryText.enableWordWrapping = false;
                secondaryText.overflowMode = TextOverflowModes.Ellipsis;

                RectTransform secondaryRect = secondaryText.rectTransform;
                secondaryRect.anchorMin = new Vector2(1f, 0.5f);
                secondaryRect.anchorMax = new Vector2(1f, 0.5f);
                secondaryRect.pivot = new Vector2(1f, 0.5f);
                secondaryRect.anchoredPosition = new Vector2(-10f, -14f);
                secondaryRect.sizeDelta = new Vector2(95f, 24f);
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