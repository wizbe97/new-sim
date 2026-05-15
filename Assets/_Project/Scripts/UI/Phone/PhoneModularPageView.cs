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

        private RectTransform activeContentRoot;
        private PhonePageLayoutMode activeLayoutMode;

        public event Action<PhonePageItemData> ItemClicked;

        public void ShowPage(
            PhonePageCatalogueSO page,
            PhonePageItemButtonView itemButtonPrefab,
            PhonePageContext context)
        {
            Clear();

            if (page == null)
            {
                ShowEmptyState("Missing page.");
                return;
            }

            if (pageTitleText != null)
            {
                pageTitleText.text = page.PageTitle;
            }

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

            foreach (PhonePageItemData item in page.Items)
            {
                if (item == null)
                {
                    continue;
                }

                PhonePageItemButtonView itemView = Instantiate(itemButtonPrefab, activeContentRoot);
                itemView.name = $"PhoneItem_{item.GetTitle()}";
                itemView.Initialize(item, context, activeLayoutMode);
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

        private bool ConfigureLayout(PhonePageCatalogueSO page)
        {
            activeLayoutMode = page.LayoutMode;

            bool useGrid = page.LayoutMode == PhonePageLayoutMode.IconGrid;

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

            listContentRoot.gameObject.SetActive(!useGrid);
            gridContentRoot.gameObject.SetActive(useGrid);

            activeContentRoot = useGrid ? gridContentRoot : listContentRoot;
            scrollRect.content = activeContentRoot;

            PrepareContentRoot(activeContentRoot);

            if (useGrid)
            {
                ConfigureGridLayout(page);
            }
            else
            {
                ConfigureListLayout(page);
            }

            return true;
        }

        private void ConfigureListLayout(PhonePageCatalogueSO page)
        {
            if (listLayoutGroup == null)
            {
                listLayoutGroup = listContentRoot.GetComponent<VerticalLayoutGroup>();
            }

            if (listContentSizeFitter == null)
            {
                listContentSizeFitter = listContentRoot.GetComponent<ContentSizeFitter>();
            }

            if (listLayoutGroup != null)
            {
                listLayoutGroup.enabled = true;
                listLayoutGroup.spacing = page.ListSpacing;
                listLayoutGroup.childAlignment = TextAnchor.UpperCenter;
                listLayoutGroup.childControlWidth = true;
                listLayoutGroup.childControlHeight = true;
                listLayoutGroup.childForceExpandWidth = true;
                listLayoutGroup.childForceExpandHeight = false;
            }

            if (listContentSizeFitter != null)
            {
                listContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                listContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private void ConfigureGridLayout(PhonePageCatalogueSO page)
        {
            if (gridLayoutGroup == null)
            {
                gridLayoutGroup = gridContentRoot.GetComponent<GridLayoutGroup>();
            }

            if (gridContentSizeFitter == null)
            {
                gridContentSizeFitter = gridContentRoot.GetComponent<ContentSizeFitter>();
            }

            if (gridLayoutGroup != null)
            {
                gridLayoutGroup.enabled = true;
                gridLayoutGroup.cellSize = page.GridCellSize;
                gridLayoutGroup.spacing = page.GridSpacing;
                gridLayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
                gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
                gridLayoutGroup.childAlignment = TextAnchor.UpperLeft;
                gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayoutGroup.constraintCount = page.GridColumnCount;
            }

            if (gridContentSizeFitter != null)
            {
                gridContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                gridContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
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

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(activeContentRoot);
        }

        private void HandleItemClicked(PhonePageItemData item)
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
            Clear();
        }
    }
}