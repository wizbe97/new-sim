using System;
using Project.Staff;
using UnityEngine;

namespace Project.UI.Phone
{
    [Serializable]
    public sealed class PhoneStaffItem : PhonePageItem
    {
        [SerializeField] private StaffMemberSO staffMember;

        public override PhonePageItemActionType ActionType => PhonePageItemActionType.HireStaffMember;
        public override StaffMemberSO StaffMember => staffMember;

        public override string GetTitle()
        {
            return staffMember != null ? staffMember.StaffName : "Missing Staff Member";
        }

        public override string GetDescription()
        {
            if (staffMember == null)
            {
                return string.Empty;
            }

            return
                $"{staffMember.Description}\n" +
                $"Daily Salary: £{staffMember.DailySalary:N0}";
        }

        public override Sprite GetIcon()
        {
            return staffMember != null ? staffMember.Icon : null;
        }

        public override string GetPrimaryText()
        {
            return staffMember != null
                ? $"Hire £{staffMember.UnlockCost:N0}"
                : "Missing Staff";
        }

        public override string GetSecondaryText()
        {
            if (staffMember == null)
            {
                return string.Empty;
            }

            return staffMember.IsUniqueHire ? "Unique Hire" : $"Level {staffMember.RequiredCasinoLevel}";
        }
    }
}