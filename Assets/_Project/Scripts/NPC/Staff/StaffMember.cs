using Project.Interfaces;
using Project.Managers;
using UnityEngine;
using UnityEngine.AI;

namespace Project.Staff
{
    [DisallowMultipleComponent]
    public sealed class StaffMember : MonoBehaviour, IInteractable
    {
        [Header("Runtime Data")]
        [SerializeField] private StaffMemberSO config;

        [Header("Navigation")]
        [SerializeField] private NavMeshAgent navMeshAgent;

        [Header("Landing Detection")]
        [SerializeField, Min(0.05f)] private float landingVelocityThreshold = 0.15f;
        [SerializeField, Min(0.05f)] private float navMeshSampleRadius = 2f;

        private Rigidbody cachedRigidbody;
        private StaffManager staffManager;
        private StaffJobController jobController;
        private bool isSkyDropping;

        public StaffMemberSO Config => config;
        public string StaffName => config != null ? config.StaffName : name;

        public bool IsSkyDropping => isSkyDropping;

        public bool IsReadyForJobs =>
            !isSkyDropping &&
            navMeshAgent != null &&
            navMeshAgent.enabled &&
            navMeshAgent.isOnNavMesh;

        public NavMeshAgent Agent => navMeshAgent;
        public StaffJobController JobController => jobController;

        public float MovementSpeed => config != null ? config.MovementSpeed : 3.5f;
        public float Acceleration => config != null ? config.Acceleration : 8f;
        public float AngularSpeed => config != null ? config.AngularSpeed : 120f;
        public float StoppingDistance => config != null ? config.StoppingDistance : 0.1f;
        public bool AutoBraking => config == null || config.AutoBraking;
        public ObstacleAvoidanceType ObstacleAvoidanceType =>
            config != null ? config.ObstacleAvoidanceType : ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        public int AvoidancePriority => config != null ? config.AvoidancePriority : 50;

        public float SlotCollectionSpeed => config != null ? config.SlotCollectionSpeed : 5f;
        public float TableGameSpeed => config != null ? config.TableGameSpeed : 5f;
        public float CleaningSpeed => config != null ? config.CleaningSpeed : 5f;
        public float RepairSpeed => config != null ? config.RepairSpeed : 5f;
        public float CashDeskSpeed => config != null ? config.CashDeskSpeed : 5f;

        public float SlotCollectionSearchIntervalSeconds =>
            config != null ? config.SlotCollectionSearchIntervalSeconds : 4f;

        public bool CollectAllSlotCash =>
            config == null || config.CollectAllSlotCash;

        public int SlotCollectionAmount =>
            config != null ? config.SlotCollectionAmount : 100;

        public bool WanderWhenNoSlotCollectionWork =>
            config == null || config.WanderWhenNoSlotCollectionWork;

        public float NoWorkWanderRadius =>
            config != null ? config.NoWorkWanderRadius : 8f;

        public float NoWorkWanderIntervalSeconds =>
            config != null ? config.NoWorkWanderIntervalSeconds : 4f;

        public float PatienceSeconds => config != null ? config.PatienceSeconds : 8f;
        public float WorkSearchRadius => config != null ? config.WorkSearchRadius : 12f;

        public string InteractionPrompt => $"Manage {StaffName}";
        public bool CanInteract => IsReadyForJobs && staffManager != null && jobController != null;

        private void Awake()
        {
            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }

            cachedRigidbody = GetComponent<Rigidbody>();
            jobController = GetComponent<StaffJobController>();
        }

        private void Update()
        {
            if (!isSkyDropping)
            {
                return;
            }

            TryCompleteSkyDrop();
        }

        public void Initialize(StaffMemberSO staffConfig, StaffManager owningStaffManager)
        {
            config = staffConfig;
            staffManager = owningStaffManager;

            if (config != null)
            {
                name = $"Staff_{config.StaffName}";
            }

            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }

            if (jobController == null)
            {
                jobController = GetComponent<StaffJobController>();
            }

            cachedRigidbody = GetComponent<Rigidbody>();

            ApplyMovementSettingsToAgent();
        }

        public int GetSlotCollectionRequestAmount(int availableAmount)
        {
            if (config != null)
            {
                return config.GetSlotCollectionRequestAmount(availableAmount);
            }

            return Mathf.Max(0, availableAmount);
        }

        public void ApplyMovementSettingsToAgent()
        {
            if (navMeshAgent == null)
            {
                return;
            }

            navMeshAgent.speed = MovementSpeed;
            navMeshAgent.acceleration = Acceleration;
            navMeshAgent.angularSpeed = AngularSpeed;
            navMeshAgent.stoppingDistance = StoppingDistance;
            navMeshAgent.autoBraking = AutoBraking;
            navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType;
            navMeshAgent.avoidancePriority = AvoidancePriority;
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            staffManager.ShowJobAssignmentPanel(this);
        }

        public void BeginSkyDrop()
        {
            isSkyDropping = true;

            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = false;
            }

            cachedRigidbody = GetComponent<Rigidbody>();

            if (cachedRigidbody != null)
            {
                cachedRigidbody.useGravity = true;
                cachedRigidbody.isKinematic = false;
            }
        }

        public void WarpTo(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);

            if (navMeshAgent == null)
            {
                return;
            }

            if (!navMeshAgent.enabled)
            {
                navMeshAgent.enabled = true;
            }

            ApplyMovementSettingsToAgent();

            if (navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.Warp(position);
            }
        }

        private void TryCompleteSkyDrop()
        {
            if (cachedRigidbody == null)
            {
                CompleteSkyDropIfNearNavMesh();
                return;
            }

            float velocityThresholdSquared = landingVelocityThreshold * landingVelocityThreshold;

            if (!cachedRigidbody.IsSleeping() &&
                cachedRigidbody.velocity.sqrMagnitude > velocityThresholdSquared)
            {
                return;
            }

            CompleteSkyDropIfNearNavMesh();
        }

        private void CompleteSkyDropIfNearNavMesh()
        {
            if (!NavMesh.SamplePosition(
                    transform.position,
                    out NavMeshHit hit,
                    navMeshSampleRadius,
                    NavMesh.AllAreas))
            {
                return;
            }

            isSkyDropping = false;

            Quaternion uprightRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            if (cachedRigidbody != null)
            {
                cachedRigidbody.velocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
                cachedRigidbody.useGravity = false;
                cachedRigidbody.isKinematic = true;
            }

            transform.SetPositionAndRotation(hit.position, uprightRotation);

            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = true;
                ApplyMovementSettingsToAgent();
                navMeshAgent.Warp(hit.position);
            }

            Debug.Log($"{name} landed and is now ready for staff AI.", this);
        }
    }
}