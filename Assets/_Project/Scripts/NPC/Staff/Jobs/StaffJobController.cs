using Project.Managers;
using Project.Staff.Jobs;
using UnityEngine;
using UnityEngine.AI;

namespace Project.Staff
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StaffMember))]
    [RequireComponent(typeof(StaffMovementController))]
    public sealed class StaffJobController : MonoBehaviour
    {
        [Header("Idle")]
        [Tooltip("Where this staff member stands when idle. This should be assigned by StaffManager from a scene reference.")]
        [SerializeField] private Transform idleStandPoint;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float destinationReachedDistance = 0.7f;
        [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 2f;

        [Header("Slot Collection Points")]
        [Tooltip("How close the staff must be to a slot collection point before starting the collection timer.")]
        [SerializeField, Min(0.05f)] private float collectionPointReachedDistance = 0.75f;

        private readonly StaffJob currentJob = new();

        private StaffMember staffMember;
        private StaffMovementController movementController;
        private NavMeshAgent agent;

        private SlotMachineManager slotMachineManager;
        private CashDeskManager cashDeskManager;

        private StaffJobContext context;

        private StaffIdleJobBehaviour idleJobBehaviour;
        private StaffSlotCollectionJobBehaviour slotCollectionJobBehaviour;
        private StaffCashDeskJobBehaviour cashDeskJobBehaviour;

        private Vector3 idleFallbackPosition;
        private bool hasIdleFallbackPosition;
        private bool isInitialized;

        public StaffJob CurrentJob => currentJob;
        public StaffJobCategory CurrentJobCategory => currentJob.Category;

        private void Awake()
        {
            staffMember = GetComponent<StaffMember>();
            movementController = GetComponent<StaffMovementController>();
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (!CanUpdateJob())
            {
                return;
            }

            GetActiveBehaviour().Tick();
        }

        public void Initialize(
            StaffMember newStaffMember,
            SlotMachineManager newSlotMachineManager,
            CashDeskManager newCashDeskManager,
            Transform newIdleStandPoint)
        {
            staffMember = newStaffMember != null ? newStaffMember : GetComponent<StaffMember>();
            movementController = GetComponent<StaffMovementController>();
            agent = staffMember != null ? staffMember.Agent : GetComponent<NavMeshAgent>();

            slotMachineManager = newSlotMachineManager;
            cashDeskManager = newCashDeskManager;
            idleStandPoint = newIdleStandPoint;

            if (staffMember == null)
            {
                Debug.LogError($"{nameof(StaffJobController)} cannot initialize because StaffMember is missing.", this);
                return;
            }

            if (movementController == null)
            {
                movementController = gameObject.AddComponent<StaffMovementController>();
            }

            if (agent == null)
            {
                Debug.LogError($"{nameof(StaffJobController)} cannot initialize because NavMeshAgent is missing.", this);
                return;
            }

            if (slotMachineManager == null)
            {
                Debug.LogError($"{nameof(StaffJobController)} cannot initialize because SlotMachineManager is missing.", this);
                return;
            }

            if (cashDeskManager == null)
            {
                Debug.LogError($"{nameof(StaffJobController)} cannot initialize because CashDeskManager is missing.", this);
                return;
            }

            movementController.Initialize(
                staffMember,
                agent,
                destinationReachedDistance,
                navMeshSampleRadius);

            CaptureIdleFallbackPosition();
            CreateJobContext();
            CreateJobBehaviours();
            ResetAllJobBehaviours();

            isInitialized = true;

            Debug.Log($"{nameof(StaffJobController)} initialized for {staffMember.StaffName}. Current job: {CurrentJobCategory}.", this);
        }

        public void SetIdleStandPoint(Transform newIdleStandPoint)
        {
            idleStandPoint = newIdleStandPoint;

            if (context != null)
            {
                context.IdleStandPoint = idleStandPoint;
            }
        }

        public void AssignJob(StaffJobCategory category)
        {
            currentJob.SetCategory(category);

            ResetAllJobBehaviours();

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }

            Debug.Log($"{staffMember.StaffName} assigned to job: {category}.", this);
        }

        private bool CanUpdateJob()
        {
            if (!isInitialized)
            {
                return false;
            }

            if (staffMember == null || !staffMember.IsReadyForJobs)
            {
                return false;
            }

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return false;
            }

            if (context == null)
            {
                return false;
            }

            return true;
        }

        private IStaffJobBehaviour GetActiveBehaviour()
        {
            switch (CurrentJobCategory)
            {
                case StaffJobCategory.SlotCollection:
                    return slotCollectionJobBehaviour;

                case StaffJobCategory.CashDesk:
                    return cashDeskJobBehaviour;

                case StaffJobCategory.TableGames:
                case StaffJobCategory.Cleaning:
                case StaffJobCategory.Repairs:
                case StaffJobCategory.Idle:
                default:
                    return idleJobBehaviour;
            }
        }

        private void CreateJobContext()
        {
            context = new StaffJobContext(
                this,
                staffMember,
                movementController,
                slotMachineManager,
                cashDeskManager,
                currentJob,
                idleStandPoint,
                idleFallbackPosition,
                hasIdleFallbackPosition);
        }

        private void CreateJobBehaviours()
        {
            StaffNoWorkWanderBehaviour noWorkWanderBehaviour =
                new StaffNoWorkWanderBehaviour(context);

            idleJobBehaviour = new StaffIdleJobBehaviour(context);

            slotCollectionJobBehaviour = new StaffSlotCollectionJobBehaviour(
                context,
                noWorkWanderBehaviour,
                collectionPointReachedDistance);

            cashDeskJobBehaviour = new StaffCashDeskJobBehaviour(context);
        }

        private void ResetAllJobBehaviours()
        {
            idleJobBehaviour?.Reset();
            slotCollectionJobBehaviour?.Reset();
            cashDeskJobBehaviour?.Reset();
        }

        private void CaptureIdleFallbackPosition()
        {
            idleFallbackPosition = transform.position;
            hasIdleFallbackPosition = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            destinationReachedDistance = Mathf.Max(0.1f, destinationReachedDistance);
            navMeshSampleRadius = Mathf.Max(0.1f, navMeshSampleRadius);
            collectionPointReachedDistance = Mathf.Max(0.05f, collectionPointReachedDistance);
        }
#endif
    }
}