using UnityEngine;
using UnityEngine.AI;

namespace Project.Staff
{
    [DisallowMultipleComponent]
    public sealed class StaffMember : MonoBehaviour
    {
        [Header("Runtime Data")]
        [SerializeField] private StaffMemberSO config;

        [Header("Navigation")]
        [SerializeField] private NavMeshAgent navMeshAgent;

        [Header("Landing Detection")]
        [SerializeField, Min(0.05f)] private float landingVelocityThreshold = 0.15f;
        [SerializeField, Min(0.05f)] private float navMeshSampleRadius = 2f;

        private Rigidbody cachedRigidbody;
        private bool isSkyDropping;

        public StaffMemberSO Config => config;
        public string StaffName => config != null ? config.StaffName : name;

        public float SlotCollectionSpeed => config != null ? config.SlotCollectionSpeed : 1f;
        public float TableGameSpeed => config != null ? config.TableGameSpeed : 1f;
        public float CleaningSpeed => config != null ? config.CleaningSpeed : 1f;
        public float RepairSpeed => config != null ? config.RepairSpeed : 1f;
        public float CashDeskSpeed => config != null ? config.CashDeskSpeed : 1f;

        private void Awake()
        {
            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }

            cachedRigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (!isSkyDropping)
            {
                return;
            }

            TryCompleteSkyDrop();
        }

        public void Initialize(StaffMemberSO staffConfig)
        {
            config = staffConfig;

            if (config != null)
            {
                name = $"Staff_{config.StaffName}";
            }
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

            if (cachedRigidbody != null)
            {
                cachedRigidbody.velocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
                cachedRigidbody.useGravity = false;
                cachedRigidbody.isKinematic = true;
            }

            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = true;
                navMeshAgent.Warp(hit.position);
            }

            transform.position = hit.position;

            Debug.Log($"{name} landed and is now ready for staff AI.", this);
        }
    }
}