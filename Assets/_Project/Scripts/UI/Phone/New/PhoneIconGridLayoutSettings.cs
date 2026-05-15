using System;
using UnityEngine;

namespace Project.UI.Phone
{
    [Serializable]
    public sealed class PhoneIconGridLayoutSettings
    {
        [SerializeField] private Vector2 cellSize = new(100f, 120f);
        [SerializeField] private Vector2 spacing = new(16f, 16f);
        [SerializeField, Min(1)] private int columnCount = 3;

        public Vector2 CellSize => cellSize;
        public Vector2 Spacing => spacing;
        public int ColumnCount => columnCount;

#if UNITY_EDITOR
        public void Validate()
        {
            cellSize.x = Mathf.Max(1f, cellSize.x);
            cellSize.y = Mathf.Max(1f, cellSize.y);
            columnCount = Mathf.Max(1, columnCount);
        }
#endif
    }
}