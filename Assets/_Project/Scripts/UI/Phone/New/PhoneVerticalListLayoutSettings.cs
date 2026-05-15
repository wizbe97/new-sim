using System;
using UnityEngine;

namespace Project.UI.Phone
{
    [Serializable]
    public sealed class PhoneVerticalListLayoutSettings
    {
        [SerializeField, Min(0f)] private float spacing = 12f;

        public float Spacing => spacing;

#if UNITY_EDITOR
        public void Validate()
        {
            spacing = Mathf.Max(0f, spacing);
        }
#endif
    }
}