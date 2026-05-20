using System;
using UnityEngine;

namespace Project.Staff
{
    [Serializable]
    public sealed class StaffJob
    {
        [SerializeField] private StaffJobCategory category = StaffJobCategory.Idle;

        public StaffJobCategory Category => category;

        public void SetCategory(StaffJobCategory newCategory)
        {
            category = newCategory;
        }
    }
}