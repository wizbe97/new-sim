using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI.Phone
{
    public sealed class PhoneModularPageView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI pageTitleText;
        [SerializeField] private TextMeshProUGUI emptyStateText;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Shop Detail Toggle")]
        [SerializeField] private Button detailToggleButton;
        [SerializeField] private TextMeshProUGUI detailToggleText;
        [SerializeField] private string detailedToggleLabel = "Detailed";
        [SerializeField] private string compactToggleLabel = "Compact";

        [Header("Content Roots")]
        [SerializeField] private RectTransform listContentRoot;
        [SerializeField] private RectTransform gridContentRoot;

        [Header("List Layout")]
        [SerializeField] private VerticalLayoutGroup listLayoutGroup;
        [SerializeField] private ContentSizeFitter listContentSizeFitter;

        [Header("Grid Layout")]
        [SerializeField] private GridLayoutGroup gridLayoutGroup;
        [SerializeField] private ContentSizeFitter gridContentSizeFitter;

        private readonly List<PhonePageItemButtonView> spawnedItems = new();
        private readonly Dictionary<string, bool> detailedViewByPageId = new();

        private RectTransform activeContentRoot;
        private PhonePageLayoutMode activeLayoutMode;
        private PhonePageCatalogueSO activePage;
        private PhonePageItemButtonView activeItemButtonPrefab;
        private PhonePageContext activeContext;

        private bool activeShowDetailedDescriptions = true;
        private int activeListColumnCount = 1;

        public event Action<PhonePageItem> ItemClicked;

        private void Awake()
        {
            if (detailToggleButton != null)
            {
                detailToggleButton.onClick.AddListener(HandleDetailToggleClicked);
                detailToggleButton.gameObject.SetActive(false);
            }
        }

        public void ShowPage(
            PhonePageCatalogueSO page,
            PhonePageItemButtonView itemButtonPrefab,
            PhonePageContext context)
        {
            Clear();

            activePage = page;
            activeItemButtonPrefab = itemButtonPrefab;
            activeContext = context;

            if (page == null)
            {
                ShowEmptyState("Missing page.");
                ConfigureDetailToggle(null);
                return;
            }

            ConfigurePageDisplayMode(page);

            if (pageTitleText != null)
            {
                pageTitleText.text = page.PageTitle;
            }

            if (scrollRect != null)
            {
                scrollRect.scrollSensitivity = page.ScrollWheelSensitivity;
            }

            ConfigureDetailToggle(page);

            if (itemButtonPrefab == null)
            {
                Debug.LogError($"{nameof(PhoneModularPageView)} on {name} is missing Item Button Prefab.", this);
                ShowEmptyState("Missing item button prefab.");
                return;
            }

            if (!ConfigureLayout(page))
            {
                ShowEmptyState("Page layout is not configured.");
                return;
            }

            if (page.Items == null || page.Items.Count == 0)
            {
                ShowEmptyState("Nothing to show.");
                ResetScrollPosition();
                return;
            }

            HideEmptyState();

            foreach (PhonePageItem item in page.Items)
            {
                if (item == null)
                {
                    continue;
                }

                PhonePageItemButtonView itemView = Instantiate(itemButtonPrefab, activeContentRoot);
                itemView.name = $"PhoneItem_{item.GetTitle()}";

                itemView.Initialize(
                    item,
                    context,
                    page.LayoutMode,
                    page.ListSettings,
                    page.GridSettings,
                    activeListColumnCount,
                    activeShowDetailedDescriptions);

                itemView.Clicked += HandleItemClicked;

                spawnedItems.Add(itemView);
            }

            RebuildActiveLayout();
            ResetScrollPosition();
        }

        public void RefreshItems()
        {
            foreach (PhonePageItemButtonView itemView in spawnedItems)
            {
                if (itemView == null)
                {
                    continue;
                }

                itemView.Refresh();
            }

            RebuildActiveLayout();
        }

        public void Clear()
        {
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                PhonePageItemButtonView itemView = spawnedItems[i];

                if (itemView == null)
                {
                    continue;
                }

                itemView.Clicked -= HandleItemClicked;
                itemView.Dispose();
                Destroy(itemView.gameObject);
            }

            spawnedItems.Clear();
        }

        private void ConfigurePageDisplayMode(PhonePageCatalogueSO page)
        {
            activeShowDetailedDescriptions = true;
            activeListColumnCount = 1;

            if (page == null)
            {
                return;
            }

            if (page.LayoutMode != PhonePageLayoutMode.VerticalList)
            {
                return;
            }

            if (page.ListSettings == null)
            {
                return;
            }

            int configuredColumnCount = Mathf.Max(1, page.ListSettings.ColumnCount);

            if (IsShopPage(page) && configuredColumnCount > 1)
            {
                activeShowDetailedDescriptions = GetDetailedViewPreference(page);
                activeListColumnCount = activeShowDetailedDescriptions
                    ? 1
                    : configuredColumnCount;

                return;
            }

            activeListColumnCount = configuredColumnCount;
            activeShowDetailedDescriptions = activeListColumnCount <= 1;
        }

        private bool ConfigureLayout(PhonePageCatalogueSO page)
        {
            activeLayoutMode = page.LayoutMode;

            if (listContentRoot == null)
            {
                Debug.LogError($"{nameof(PhoneModularPageView)} on {name} is missing List Content Root.", this);
                return false;
            }

            if (gridContentRoot == null)
            {
                Debug.LogError($"{nameof(PhoneModularPageView)} on {name} is missing Grid Content Root.", this);
                return false;
            }

            if (scrollRect == null)
            {
                Debug.LogError($"{nameof(PhoneModularPageView)} on {name} is missing Scroll Rect.", this);
                return false;
            }

            bool isIconGrid = page.LayoutMode == PhonePageLayoutMode.IconGrid;
            bool isListColumns = page.LayoutMode == PhonePageLayoutMode.VerticalList &&
                                 activeListColumnCount > 1;

            bool useGridRoot = isIconGrid || isListColumns;

            listContentRoot.gameObject.SetActive(!useGridRoot);
            gridContentRoot.gameObject.SetActive(useGridRoot);

            activeContentRoot = useGridRoot ? gridContentRoot : listContentRoot;
            scrollRect.content = activeContentRoot;

            PrepareContentRoot(activeContentRoot);

            if (isIconGrid)
            {
                ConfigureIconGridLayout(page);
            }
            else if (isListColumns)
            {
                ConfigureListColumnLayout(page);
            }
            else
            {
                ConfigureSingleColumnListLayout(page);
            }

            return true;
        }

        private void ConfigureSingleColumnListLayout(PhonePageCatalogueSO page)
        {
            if (listLayoutGroup == null)
            {
                listLayoutGroup = listContentRoot.GetComponent<VerticalLayoutGroup>();
            }

            if (listContentSizeFitter == null)
            {
                listContentSizeFitter = listContentRoot.GetComponent<ContentSizeFitter>();
            }

            if (gridLayoutGroup != null)
            {
                gridLayoutGroup.enabled = false;
            }

            PhoneVerticalListLayoutSettings settings = page.ListSettings;

            if (settings == null)
            {
                Debug.LogError($"{nameof(PhoneModularPageView)} on {name} cannot configure list layout because List Settings are missing.", this);
                return;
            }

            if (listLayoutGroup != null)
            {
                listLayoutGroup.enabled = true;
                listLayoutGroup.padding = settings.Padding.ToRectOffset();
                listLayoutGroup.spacing = settings.Spacing;
                listLayoutGroup.childAlignment = TextAnchor.UpperCenter;
                listLayoutGroup.childControlWidth = true;
                listLayoutGroup.childControlHeight = true;
                listLayoutGroup.childForceExpandWidth = true;
                listLayoutGroup.childForceExpandHeight = false;
            }

            if (listContentSizeFitter != null)
            {
                listContentSizeFitter.enabled = true;
                listContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                listContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            if (gridContentSizeFitter != null)
            {
                gridContentSizeFitter.enabled = false;
            }
        }

        private void ConfigureListColumnLayout(PhonePageCatalogueSO page)
        {
            if (gridLayoutGroup == null)
            {
                gridLayoutGroup = gridContentRoot.GetComponent<GridLayoutGroup>();
            }

            if (gridContentSizeFitter == null)
            {
                gridContentSizeFitter = gridContentRoot.GetComponent<ContentSizeFitter>();
            }

            if (listLayoutGroup != null)
            {
                listLayoutGroup.enabled = false;
            }

            PhoneVerticalListLayoutSettings settings = page.ListSettings;

            if (settings == null)
            {
                Debug.LogError($"{nameof(PhoneModularPageView)} on {name} cannot configure list columns because List Settings are missing.", this);
                return;
            }

            if (gridLayoutGroup != null)
            {
                gridLayoutGroup.enabled = true;
                gridLayoutGroup.padding = settings.Padding.ToRectOffset();
                gridLayoutGroup.cellSize = GetListColumnCellSize(settings);
                gridLayoutGroup.spacing = new Vector2(settings.Spacing, settings.Spacing);
                gridLayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
                gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
                gridLayoutGroup.childAlignment = TextAnchor.UpperLeft;
                gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayoutGroup.constraintCount = activeListColumnCount;
            }
            else
            {
                Debug.LogError(
                    $"{nameof(PhoneModularPageView)} on {name} needs a GridLayoutGroup on Grid Content Root to use list columns.",
                    this);
            }

            if (gridContentSizeFitter != null)
            {
                gridContentSizeFitter.enabled = true;
                gridContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                gridContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            if (listContentSizeFitter != null)
            {
                listContentSizeFitter.enabled = false;
            }
        }

        private void ConfigureIconGridLayout(PhonePageCatalogueSO page)
        {
            if (gridLayoutGroup == null)
            {
                gridLayoutGroup = gridContentRoot.GetComponent<GridLayoutGroup>();
            }

            if (gridContentSizeFitter == null)
            {
                gridContentSizeFitter = gridContentRoot.GetComponent<ContentSizeFitter>();
            }

            if (listLayoutGroup != null)
            {
                listLayoutGroup.enabled = false;
            }

            PhoneIconGridLayoutSettings settings = page.GridSettings;

            if (settings == null)
            {
                Debug.LogError($"{nameof(PhoneModularPageView)} on {name} cannot configure icon grid because Grid Settings are missing.", this);
                return;
            }

            if (gridLayoutGroup != null)
            {
                gridLayoutGroup.enabled = true;
                gridLayoutGroup.padding = settings.Padding.ToRectOffset();
                gridLayoutGroup.cellSize = settings.CellSize;
                gridLayoutGroup.spacing = settings.Spacing;
                gridLayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
                gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
                gridLayoutGroup.childAlignment = TextAnchor.UpperLeft;
                gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayoutGroup.constraintCount = settings.ColumnCount;
            }

            if (gridContentSizeFitter != null)
            {
                gridContentSizeFitter.enabled = true;
                gridContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                gridContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            if (listContentSizeFitter != null)
            {
                listContentSizeFitter.enabled = false;
            }
        }

        private bool ShouldShowDetailToggle(PhonePageCatalogueSO page)
        {
            return page != null &&
                   IsShopPage(page) &&
                   page.LayoutMode == PhonePageLayoutMode.VerticalList &&
                   page.ListSettings != null &&
                   page.ListSettings.ColumnCount > 1;
        }

        private void ConfigureDetailToggle(PhonePageCatalogueSO page)
        {
            bool canToggle = ShouldShowDetailToggle(page);

            if (detailToggleButton != null)
            {
                detailToggleButton.gameObject.SetActive(canToggle);
            }

            if (detailToggleText != null)
            {
                detailToggleText.text = activeShowDetailedDescriptions
                    ? detailedToggleLabel
                    : compactToggleLabel;
            }
        }

        private void HandleDetailToggleClicked()
        {
            if (activePage == null)
            {
                return;
            }

            if (!ShouldShowDetailToggle(activePage))
            {
                return;
            }

            bool current = GetDetailedViewPreference(activePage);
            SetDetailedViewPreference(activePage, !current);

            ShowPage(activePage, activeItemButtonPrefab, activeContext);
        }

        private bool GetDetailedViewPreference(PhonePageCatalogueSO page)
        {
            if (page == null)
            {
                return true;
            }

            string key = GetPagePreferenceKey(page);

            if (detailedViewByPageId.TryGetValue(key, out bool showDetailed))
            {
                return showDetailed;
            }

            detailedViewByPageId[key] = true;
            return true;
        }

        private void SetDetailedViewPreference(PhonePageCatalogueSO page, bool showDetailed)
        {
            if (page == null)
            {
                return;
            }

            detailedViewByPageId[GetPagePreferenceKey(page)] = showDetailed;
        }

        private static string GetPagePreferenceKey(PhonePageCatalogueSO page)
        {
            if (page == null)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(page.PageId)
                ? page.name
                : page.PageId;
        }

        private static bool IsShopPage(PhonePageCatalogueSO page)
        {
            return page != null &&
                   string.Equals(page.PageId, "shop", StringComparison.OrdinalIgnoreCase);
        }

        private Vector2 GetListColumnCellSize(PhoneVerticalListLayoutSettings settings)
        {
            RectOffset padding = settings.Padding.ToRectOffset();

            float availableWidth = GetViewportWidth();

            availableWidth -= padding.left;
            availableWidth -= padding.right;
            availableWidth -= settings.Spacing * (activeListColumnCount - 1);

            float cellWidth = availableWidth / activeListColumnCount;
            cellWidth = Mathf.Max(1f, cellWidth);

            return new Vector2(cellWidth, settings.ItemHeight);
        }

        private float GetViewportWidth()
        {
            if (scrollRect != null && scrollRect.viewport != null)
            {
                float viewportWidth = scrollRect.viewport.rect.width;

                if (viewportWidth > 0f)
                {
                    return viewportWidth;
                }
            }

            if (activeContentRoot != null)
            {
                float contentWidth = activeContentRoot.rect.width;

                if (contentWidth > 0f)
                {
                    return contentWidth;
                }
            }

            return 1f;
        }

        private void PrepareContentRoot(RectTransform contentRoot)
        {
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.offsetMin = new Vector2(0f, contentRoot.offsetMin.y);
            contentRoot.offsetMax = new Vector2(0f, contentRoot.offsetMax.y);
            contentRoot.sizeDelta = new Vector2(contentRoot.sizeDelta.x, 0f);
        }

        private void RebuildActiveLayout()
        {
            if (activeContentRoot == null)
            {
                return;
            }

            RefreshListColumnCellSizeIfNeeded();

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(activeContentRoot);
        }

        private void RefreshListColumnCellSizeIfNeeded()
        {
            if (activePage == null)
            {
                return;
            }

            if (activePage.LayoutMode != PhonePageLayoutMode.VerticalList)
            {
                return;
            }

            if (activeListColumnCount <= 1)
            {
                return;
            }

            if (gridLayoutGroup == null || !gridLayoutGroup.enabled)
            {
                return;
            }

            gridLayoutGroup.cellSize = GetListColumnCellSize(activePage.ListSettings);
        }

        private void HandleItemClicked(PhonePageItem item)
        {
            ItemClicked?.Invoke(item);
        }

        private void ShowEmptyState(string message)
        {
            if (emptyStateText == null)
            {
                return;
            }

            emptyStateText.gameObject.SetActive(true);
            emptyStateText.text = message;
        }

        private void HideEmptyState()
        {
            if (emptyStateText == null)
            {
                return;
            }

            emptyStateText.text = string.Empty;
            emptyStateText.gameObject.SetActive(false);
        }

        private void ResetScrollPosition()
        {
            if (scrollRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private void OnDestroy()
        {
            if (detailToggleButton != null)
            {
                detailToggleButton.onClick.RemoveListener(HandleDetailToggleClicked);
            }

            Clear();
        }
    }
}