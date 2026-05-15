using System.Collections.Generic;
using UnityEngine;

namespace Project.UI.Phone
{
    [CreateAssetMenu(
        fileName = "PhonePageCatalogue",
        menuName = "Project/UI/Phone/Page Catalogue")]
    public sealed class PhonePageCatalogueSO : ScriptableObject
    {
        [Header("Page Identity")]
        [SerializeField] private string pageId = "new_page";
        [SerializeField] private string pageTitle = "New Page";
        [SerializeField, TextArea(2, 5)] private string pageDescription = "Page description.";
        [SerializeField] private Sprite pageIcon;

        [Header("Layout")]
        [SerializeField] private PhonePageLayoutMode layoutMode = PhonePageLayoutMode.VerticalList;

        [Header("Vertical List Settings")]
        [SerializeField, Min(0f)] private float listSpacing = 12f;

        [Header("Icon Grid Settings")]
        [SerializeField] private Vector2 gridCellSize = new Vector2(100f, 120f);
        [SerializeField] private Vector2 gridSpacing = new Vector2(16f, 16f);
        [SerializeField, Min(1)] private int gridColumnCount = 3;

        [Header("Items")]
        [SerializeField] private List<PhonePageItemData> items = new();

        public string PageId => pageId;
        public string PageTitle => pageTitle;
        public string PageDescription => pageDescription;
        public Sprite PageIcon => pageIcon;
        public PhonePageLayoutMode LayoutMode => layoutMode;
        public float ListSpacing => listSpacing;
        public Vector2 GridCellSize => gridCellSize;
        public Vector2 GridSpacing => gridSpacing;
        public int GridColumnCount => gridColumnCount;
        public IReadOnlyList<PhonePageItemData> Items => items;

        public PhonePageItemData GetItem(int index)
        {
            if (index < 0 || index >= items.Count)
            {
                return null;
            }

            return items[index];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(pageId))
            {
                pageId = name;
            }

            pageId = pageId.Trim().ToLowerInvariant().Replace(" ", "_");

            if (string.IsNullOrWhiteSpace(pageTitle))
            {
                pageTitle = name;
            }

            gridCellSize.x = Mathf.Max(1f, gridCellSize.x);
            gridCellSize.y = Mathf.Max(1f, gridCellSize.y);
            gridColumnCount = Mathf.Max(1, gridColumnCount);

            foreach (PhonePageItemData item in items)
            {
                item?.Validate();
            }
        }
#endif
    }
}