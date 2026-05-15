using System;
using UnityEngine;

namespace Project.UI.Phone
{
    [Serializable]
    public sealed class PhoneLayoutPadding
    {
        [SerializeField, Min(0)] private int left;
        [SerializeField, Min(0)] private int right;
        [SerializeField, Min(0)] private int top;
        [SerializeField, Min(0)] private int bottom;

        public int Left => left;
        public int Right => right;
        public int Top => top;
        public int Bottom => bottom;

        public RectOffset ToRectOffset()
        {
            return new RectOffset(left, right, top, bottom);
        }

        public void ResetToZero()
        {
            left = 0;
            right = 0;
            top = 0;
            bottom = 0;
        }

#if UNITY_EDITOR
        public void Validate()
        {
            left = Mathf.Max(0, left);
            right = Mathf.Max(0, right);
            top = Mathf.Max(0, top);
            bottom = Mathf.Max(0, bottom);
        }
#endif
    }
}