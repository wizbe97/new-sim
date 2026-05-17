using UnityEngine;

namespace Project.Staff
{
    [CreateAssetMenu(
        fileName = "StaffMember",
        menuName = "Project/Staff/Staff Member")]
    public sealed class StaffMemberSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string staffId = "new_staff_member";
        [SerializeField] private string staffName = "New Staff Member";
        [SerializeField, TextArea(2, 5)] private string description = "Staff member description.";
        [SerializeField] private Sprite icon;

        [Header("Prefab")]
        [SerializeField] private StaffMember prefab;

        [Header("Unlocking")]
        [SerializeField, Min(0)] private int unlockCost = 250;
        [SerializeField, Min(1)] private int requiredCasinoLevel = 1;
        [SerializeField] private bool isUniqueHire = false;

        [Header("Salary")]
        [SerializeField, Min(0)] private int dailySalary = 50;

        [Header("Work Speeds")]
        [Tooltip("Higher values mean this staff member collects slot machine cash faster.")]
        [SerializeField, Min(0f)] private float slotCollectionSpeed = 1f;

        [Tooltip("Higher values mean this staff member handles table games faster later.")]
        [SerializeField, Min(0f)] private float tableGameSpeed = 1f;

        [Tooltip("Higher values mean this staff member cleans mess faster later.")]
        [SerializeField, Min(0f)] private float cleaningSpeed = 1f;

        [Tooltip("Higher values mean this staff member repairs machines faster later.")]
        [SerializeField, Min(0f)] private float repairSpeed = 1f;

        [Tooltip("Higher values mean this staff member handles cash desk tasks faster later.")]
        [SerializeField, Min(0f)] private float cashDeskSpeed = 1f;

        [Header("Behaviour")]
        [Tooltip("How patient this staff member is before returning to idle behaviour.")]
        [SerializeField, Min(0f)] private float patienceSeconds = 8f;

        [Tooltip("How far this staff member can search for work later.")]
        [SerializeField, Min(0f)] private float workSearchRadius = 12f;

        public string StaffId => staffId;
        public string StaffName => staffName;
        public string Description => description;
        public Sprite Icon => icon;

        public StaffMember Prefab => prefab;

        public int UnlockCost => unlockCost;
        public int RequiredCasinoLevel => requiredCasinoLevel;
        public bool IsUniqueHire => isUniqueHire;

        public int DailySalary => dailySalary;

        public float SlotCollectionSpeed => slotCollectionSpeed;
        public float TableGameSpeed => tableGameSpeed;
        public float CleaningSpeed => cleaningSpeed;
        public float RepairSpeed => repairSpeed;
        public float CashDeskSpeed => cashDeskSpeed;

        public float PatienceSeconds => patienceSeconds;
        public float WorkSearchRadius => workSearchRadius;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(staffId))
            {
                staffId = name;
            }

            staffId = staffId.Trim().ToLowerInvariant().Replace(" ", "_");

            if (string.IsNullOrWhiteSpace(staffName))
            {
                staffName = name;
            }

            unlockCost = Mathf.Max(0, unlockCost);
            requiredCasinoLevel = Mathf.Max(1, requiredCasinoLevel);
            dailySalary = Mathf.Max(0, dailySalary);

            slotCollectionSpeed = Mathf.Max(0f, slotCollectionSpeed);
            tableGameSpeed = Mathf.Max(0f, tableGameSpeed);
            cleaningSpeed = Mathf.Max(0f, cleaningSpeed);
            repairSpeed = Mathf.Max(0f, repairSpeed);
            cashDeskSpeed = Mathf.Max(0f, cashDeskSpeed);

            patienceSeconds = Mathf.Max(0f, patienceSeconds);
            workSearchRadius = Mathf.Max(0f, workSearchRadius);
        }
#endif
    }
}