using UnityEngine;
using UnityEngine.AI;

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

        [Header("Movement")]
        [Tooltip("NavMeshAgent movement speed in units per second.")]
        [SerializeField, Min(0.1f)] private float movementSpeed = 3.5f;

        [Tooltip("How quickly this staff member accelerates to movement speed.")]
        [SerializeField, Min(0.1f)] private float acceleration = 8f;

        [Tooltip("How quickly this staff member turns while moving.")]
        [SerializeField, Min(0f)] private float angularSpeed = 120f;

        [Tooltip("How close this staff member gets to a destination before stopping.")]
        [SerializeField, Min(0f)] private float stoppingDistance = 0.1f;

        [Tooltip("Whether the NavMeshAgent slows down automatically near its destination.")]
        [SerializeField] private bool autoBraking = true;

        [Tooltip("Obstacle avoidance quality used by the NavMeshAgent.")]
        [SerializeField] private ObstacleAvoidanceType obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        [Tooltip("Lower values have higher avoidance priority. Unity's normal range is 0 to 99.")]
        [SerializeField, Range(0, 99)] private int avoidancePriority = 50;

        [Header("Work Durations")]
        [Tooltip("Seconds required to collect slot machine cash after reaching the collection point.")]
        [SerializeField, Min(0f)] private float slotCollectionSpeed = 5f;

        [Tooltip("Seconds required to handle table game tasks later.")]
        [SerializeField, Min(0f)] private float tableGameSpeed = 5f;

        [Tooltip("Seconds required to clean mess later.")]
        [SerializeField, Min(0f)] private float cleaningSpeed = 5f;

        [Tooltip("Seconds required to repair machines later.")]
        [SerializeField, Min(0f)] private float repairSpeed = 5f;

        [Tooltip("Seconds required to handle cash desk payouts after the staff member reaches the cash desk.")]
        [SerializeField, Min(0f)] private float cashDeskSpeed = 5f;

        [Header("Slot Collection")]
        [Tooltip("How many seconds this staff member waits before checking again when no reachable slot machine needs collecting.")]
        [SerializeField, Min(0.1f)] private float slotCollectionSearchIntervalSeconds = 4f;

        [Tooltip("If enabled, this staff member collects all available cash from the slot machine.")]
        [SerializeField] private bool collectAllSlotCash = true;

        [Tooltip("Used only when Collect All Slot Cash is disabled.")]
        [SerializeField, Min(1)] private int slotCollectionAmount = 100;

        [Header("No Work Wandering")]
        [Tooltip("If enabled, this staff member wanders while assigned to slot collection but no reachable slot machine needs collecting.")]
        [SerializeField] private bool wanderWhenNoSlotCollectionWork = true;

        [Tooltip("How far this staff member can wander while waiting for slot collection work.")]
        [SerializeField, Min(0.5f)] private float noWorkWanderRadius = 8f;

        [Tooltip("How often this staff member chooses a new wander point when no slot collection work is available.")]
        [SerializeField, Min(0.5f)] private float noWorkWanderIntervalSeconds = 4f;

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

        public float MovementSpeed => movementSpeed;
        public float Acceleration => acceleration;
        public float AngularSpeed => angularSpeed;
        public float StoppingDistance => stoppingDistance;
        public bool AutoBraking => autoBraking;
        public ObstacleAvoidanceType ObstacleAvoidanceType => obstacleAvoidanceType;
        public int AvoidancePriority => avoidancePriority;

        public float SlotCollectionSpeed => slotCollectionSpeed;
        public float TableGameSpeed => tableGameSpeed;
        public float CleaningSpeed => cleaningSpeed;
        public float RepairSpeed => repairSpeed;
        public float CashDeskSpeed => cashDeskSpeed;

        public float SlotCollectionSearchIntervalSeconds => slotCollectionSearchIntervalSeconds;
        public bool CollectAllSlotCash => collectAllSlotCash;
        public int SlotCollectionAmount => slotCollectionAmount;

        public bool WanderWhenNoSlotCollectionWork => wanderWhenNoSlotCollectionWork;
        public float NoWorkWanderRadius => noWorkWanderRadius;
        public float NoWorkWanderIntervalSeconds => noWorkWanderIntervalSeconds;

        public float PatienceSeconds => patienceSeconds;
        public float WorkSearchRadius => workSearchRadius;

        public int GetSlotCollectionRequestAmount(int availableAmount)
        {
            availableAmount = Mathf.Max(0, availableAmount);

            if (collectAllSlotCash)
            {
                return availableAmount;
            }

            return Mathf.Clamp(slotCollectionAmount, 0, availableAmount);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrWhiteSpace(staffId))
            {
                staffId = staffId.Trim().ToLowerInvariant().Replace(" ", "_");
            }

            if (string.IsNullOrWhiteSpace(staffName))
            {
                staffName = "New Staff Member";
            }

            unlockCost = Mathf.Max(0, unlockCost);
            requiredCasinoLevel = Mathf.Max(1, requiredCasinoLevel);
            dailySalary = Mathf.Max(0, dailySalary);

            movementSpeed = Mathf.Max(0.1f, movementSpeed);
            acceleration = Mathf.Max(0.1f, acceleration);
            angularSpeed = Mathf.Max(0f, angularSpeed);
            stoppingDistance = Mathf.Max(0f, stoppingDistance);
            avoidancePriority = Mathf.Clamp(avoidancePriority, 0, 99);

            slotCollectionSpeed = Mathf.Max(0f, slotCollectionSpeed);
            tableGameSpeed = Mathf.Max(0f, tableGameSpeed);
            cleaningSpeed = Mathf.Max(0f, cleaningSpeed);
            repairSpeed = Mathf.Max(0f, repairSpeed);
            cashDeskSpeed = Mathf.Max(0f, cashDeskSpeed);

            slotCollectionSearchIntervalSeconds = Mathf.Max(0.1f, slotCollectionSearchIntervalSeconds);
            slotCollectionAmount = Mathf.Max(1, slotCollectionAmount);

            noWorkWanderRadius = Mathf.Max(0.5f, noWorkWanderRadius);
            noWorkWanderIntervalSeconds = Mathf.Max(0.5f, noWorkWanderIntervalSeconds);

            patienceSeconds = Mathf.Max(0f, patienceSeconds);
            workSearchRadius = Mathf.Max(0f, workSearchRadius);
        }
#endif
    }
}