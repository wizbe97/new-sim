using System;
using UnityEngine;

namespace Project.UI.Phone
{
    [Serializable]
    public sealed class PhoneVerticalListLayoutSettings
    {
        private const int CurrentSettingsVersion = 2;

        [SerializeField, HideInInspector] private int settingsVersion;

        [Header("Content Layout")]
        [SerializeField] private PhoneLayoutPadding padding = new();
        [SerializeField, Min(0f)] private float spacing = 12f;

        [Header("Item Size")]
        [SerializeField, Min(1f)] private float itemHeight = 96f;

        [Header("Icon")]
        [SerializeField] private Vector2 iconSize = new(54f, 54f);
        [SerializeField] private Vector2 iconAnchoredPosition = new(14f, 0f);

        [Header("Title Text")]
        [SerializeField, Min(1f)] private float titleFontSize = 20f;
        [SerializeField] private Vector2 titleOffsetMin = new(82f, -36f);
        [SerializeField] private Vector2 titleOffsetMax = new(-150f, -8f);

        [Header("Description Text")]
        [SerializeField, Min(1f)] private float descriptionFontSize = 13f;
        [SerializeField] private Vector2 descriptionOffsetMin = new(82f, 10f);
        [SerializeField] private Vector2 descriptionOffsetMax = new(-150f, -38f);

        [Header("Primary Text")]
        [SerializeField, Min(1f)] private float primaryFontSize = 15f;
        [SerializeField] private Vector2 primaryAnchoredPosition = new(-10f, 12f);
        [SerializeField] private Vector2 primarySize = new(135f, 28f);

        [Header("Secondary Text")]
        [SerializeField, Min(1f)] private float secondaryFontSize = 12f;
        [SerializeField] private Vector2 secondaryAnchoredPosition = new(-10f, -14f);
        [SerializeField] private Vector2 secondarySize = new(135f, 24f);

        public PhoneLayoutPadding Padding => padding;
        public float Spacing => spacing;
        public float ItemHeight => itemHeight;

        public Vector2 IconSize => iconSize;
        public Vector2 IconAnchoredPosition => iconAnchoredPosition;

        public float TitleFontSize => titleFontSize;
        public Vector2 TitleOffsetMin => titleOffsetMin;
        public Vector2 TitleOffsetMax => titleOffsetMax;

        public float DescriptionFontSize => descriptionFontSize;
        public Vector2 DescriptionOffsetMin => descriptionOffsetMin;
        public Vector2 DescriptionOffsetMax => descriptionOffsetMax;

        public float PrimaryFontSize => primaryFontSize;
        public Vector2 PrimaryAnchoredPosition => primaryAnchoredPosition;
        public Vector2 PrimarySize => primarySize;

        public float SecondaryFontSize => secondaryFontSize;
        public Vector2 SecondaryAnchoredPosition => secondaryAnchoredPosition;
        public Vector2 SecondarySize => secondarySize;

        public void EnsureDefaults()
        {
            padding ??= new PhoneLayoutPadding();

            if (settingsVersion == CurrentSettingsVersion)
            {
                return;
            }

            if (settingsVersion <= 0)
            {
                padding.ResetToZero();

                if (spacing <= 0f)
                {
                    spacing = 12f;
                }

                itemHeight = 96f;

                iconSize = new Vector2(54f, 54f);
                iconAnchoredPosition = new Vector2(14f, 0f);

                titleFontSize = 20f;
                titleOffsetMin = new Vector2(82f, -36f);
                titleOffsetMax = new Vector2(-150f, -8f);

                descriptionFontSize = 13f;
                descriptionOffsetMin = new Vector2(82f, 10f);
                descriptionOffsetMax = new Vector2(-150f, -38f);

                primaryFontSize = 15f;
                primaryAnchoredPosition = new Vector2(-10f, 12f);
                primarySize = new Vector2(135f, 28f);

                secondaryFontSize = 12f;
                secondaryAnchoredPosition = new Vector2(-10f, -14f);
                secondarySize = new Vector2(135f, 24f);
            }
            else if (settingsVersion == 1)
            {
                if (Approximately(primarySize.x, 95f))
                {
                    primarySize.x = 135f;
                }

                if (Approximately(secondarySize.x, 95f))
                {
                    secondarySize.x = 135f;
                }

                if (Approximately(titleOffsetMax.x, -110f))
                {
                    titleOffsetMax.x = -150f;
                }

                if (Approximately(descriptionOffsetMax.x, -110f))
                {
                    descriptionOffsetMax.x = -150f;
                }
            }

            settingsVersion = CurrentSettingsVersion;
        }

        private static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) <= 0.01f;
        }

#if UNITY_EDITOR
        public void Validate()
        {
            EnsureDefaults();

            padding ??= new PhoneLayoutPadding();
            padding.Validate();

            spacing = Mathf.Max(0f, spacing);
            itemHeight = Mathf.Max(1f, itemHeight);

            iconSize.x = Mathf.Max(1f, iconSize.x);
            iconSize.y = Mathf.Max(1f, iconSize.y);

            titleFontSize = Mathf.Max(1f, titleFontSize);
            descriptionFontSize = Mathf.Max(1f, descriptionFontSize);
            primaryFontSize = Mathf.Max(1f, primaryFontSize);
            secondaryFontSize = Mathf.Max(1f, secondaryFontSize);

            primarySize.x = Mathf.Max(1f, primarySize.x);
            primarySize.y = Mathf.Max(1f, primarySize.y);

            secondarySize.x = Mathf.Max(1f, secondarySize.x);
            secondarySize.y = Mathf.Max(1f, secondarySize.y);
        }
#endif
    }
}