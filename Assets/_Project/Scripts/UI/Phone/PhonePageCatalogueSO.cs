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

        [SerializeField] private PhoneVerticalListLayoutSettings listSettings = new();
        [SerializeField] private PhoneIconGridLayoutSettings gridSettings = new();

        [Header("Items")]
        [SerializeReference] private List<PhonePageItem> items = new();

        public string PageId => pageId;
        public string PageTitle => pageTitle;
        public string PageDescription => pageDescription;
        public Sprite PageIcon => pageIcon;

        public PhonePageLayoutMode LayoutMode => layoutMode;
        public PhoneVerticalListLayoutSettings ListSettings => listSettings;
        public PhoneIconGridLayoutSettings GridSettings => gridSettings;

        public IReadOnlyList<PhonePageItem> Items => items;

        public PhonePageItem GetItem(int index)
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

            listSettings ??= new PhoneVerticalListLayoutSettings();
            gridSettings ??= new PhoneIconGridLayoutSettings();

            listSettings.Validate();
            gridSettings.Validate();

            foreach (PhonePageItem item in items)
            {
                item?.Validate();
            }
        }
#endif
    }
}