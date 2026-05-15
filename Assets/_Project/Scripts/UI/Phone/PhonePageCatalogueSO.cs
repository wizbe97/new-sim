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

        [Header("Scrolling")]
        [SerializeField, Min(0f)] private float scrollWheelSensitivity = 30f;

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

        public float ScrollWheelSensitivity => scrollWheelSensitivity;

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

        private void OnEnable()
        {
            EnsureLayoutSettings();
        }

        private void EnsureLayoutSettings()
        {
            listSettings ??= new PhoneVerticalListLayoutSettings();
            gridSettings ??= new PhoneIconGridLayoutSettings();

            listSettings.EnsureDefaults();
            gridSettings.EnsureDefaults();

            if (scrollWheelSensitivity <= 0f)
            {
                scrollWheelSensitivity = 30f;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureLayoutSettings();

            if (string.IsNullOrWhiteSpace(pageId))
            {
                pageId = name;
            }

            pageId = pageId.Trim().ToLowerInvariant().Replace(" ", "_");

            if (string.IsNullOrWhiteSpace(pageTitle))
            {
                pageTitle = name;
            }

            scrollWheelSensitivity = Mathf.Max(0f, scrollWheelSensitivity);

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