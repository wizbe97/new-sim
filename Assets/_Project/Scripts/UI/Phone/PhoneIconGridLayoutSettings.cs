using System;
using UnityEngine;

namespace Project.UI.Phone
{
    [Serializable]
    public sealed class PhoneIconGridLayoutSettings
    {
        private const int CurrentSettingsVersion = 1;

        [SerializeField, HideInInspector] private int settingsVersion;

        [Header("Content Layout")]
        [SerializeField] private PhoneLayoutPadding padding = new();

        [Header("Grid")]
        [SerializeField] private Vector2 cellSize = new(100f, 120f);
        [SerializeField] private Vector2 spacing = new(16f, 16f);
        [SerializeField, Min(1)] private int columnCount = 3;

        [Header("Icon")]
        [SerializeField] private Vector2 iconSize = new(54f, 54f);
        [SerializeField] private Vector2 iconAnchoredPosition = new(0f, -10f);

        [Header("Title Text")]
        [SerializeField, Min(1f)] private float titleFontSize = 13f;
        [SerializeField] private Vector2 titleOffsetMin = new(6f, 8f);
        [SerializeField] private Vector2 titleOffsetMax = new(-6f, 44f);

        public PhoneLayoutPadding Padding => padding;

        public Vector2 CellSize => cellSize;
        public Vector2 Spacing => spacing;
        public int ColumnCount => columnCount;

        public Vector2 IconSize => iconSize;
        public Vector2 IconAnchoredPosition => iconAnchoredPosition;

        public float TitleFontSize => titleFontSize;
        public Vector2 TitleOffsetMin => titleOffsetMin;
        public Vector2 TitleOffsetMax => titleOffsetMax;

        public void EnsureDefaults()
        {
            padding ??= new PhoneLayoutPadding();

            if (settingsVersion == CurrentSettingsVersion)
            {
                return;
            }

            padding.ResetToZero();

            if (cellSize.x <= 0f || cellSize.y <= 0f)
            {
                cellSize = new Vector2(100f, 120f);
            }

            if (spacing.x <= 0f && spacing.y <= 0f)
            {
                spacing = new Vector2(16f, 16f);
            }

            if (columnCount <= 0)
            {
                columnCount = 3;
            }

            iconSize = new Vector2(54f, 54f);
            iconAnchoredPosition = new Vector2(0f, -10f);

            titleFontSize = 13f;
            titleOffsetMin = new Vector2(6f, 8f);
            titleOffsetMax = new Vector2(-6f, 44f);

            settingsVersion = CurrentSettingsVersion;
        }

#if UNITY_EDITOR
        public void Validate()
        {
            EnsureDefaults();

            padding ??= new PhoneLayoutPadding();
            padding.Validate();

            cellSize.x = Mathf.Max(1f, cellSize.x);
            cellSize.y = Mathf.Max(1f, cellSize.y);

            spacing.x = Mathf.Max(0f, spacing.x);
            spacing.y = Mathf.Max(0f, spacing.y);

            columnCount = Mathf.Max(1, columnCount);

            iconSize.x = Mathf.Max(1f, iconSize.x);
            iconSize.y = Mathf.Max(1f, iconSize.y);

            titleFontSize = Mathf.Max(1f, titleFontSize);
        }
#endif
    }
}