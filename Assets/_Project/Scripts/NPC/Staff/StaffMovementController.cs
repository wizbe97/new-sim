using UnityEngine;
using UnityEngine.AI;

namespace Project.Staff
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class StaffMovementController : MonoBehaviour
    {
        private StaffMember staffMember;
        private NavMeshAgent agent;
        private float destinationReachedDistance = 0.7f;
        private float navMeshSampleRadius = 2f;

        public NavMeshAgent Agent => agent;

        public void Initialize(
            StaffMember newStaffMember,
            NavMeshAgent newAgent,
            float newDestinationReachedDistance,
            float newNavMeshSampleRadius)
        {
            staffMember = newStaffMember;
            agent = newAgent != null ? newAgent : GetComponent<NavMeshAgent>();

            destinationReachedDistance = Mathf.Max(0.1f, newDestinationReachedDistance);
            navMeshSampleRadius = Mathf.Max(0.1f, newNavMeshSampleRadius);

            ApplyStaffMovementSettings();
        }

        public void ApplyStaffMovementSettings()
        {
            if (staffMember == null)
            {
                return;
            }

            staffMember.ApplyMovementSettingsToAgent();
        }

        public bool SetDestination(Vector3 destination)
        {
            if (!CanUseAgent())
            {
                return false;
            }

            if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                return false;
            }

            ApplyStaffMovementSettings();

            agent.isStopped = false;
            agent.SetDestination(hit.position);

            return true;
        }

        public void Stop()
        {
            if (!CanUseAgent())
            {
                return;
            }

            agent.ResetPath();
        }

        public bool HasReachedPosition(Vector3 position)
        {
            return HasReachedPosition(position, destinationReachedDistance);
        }

        public bool HasReachedPosition(Vector3 position, float reachDistance)
        {
            float distanceSquared = (transform.position - position).sqrMagnitude;
            return distanceSquared <= reachDistance * reachDistance;
        }

        public bool HasAgentReachedDestination()
        {
            if (!CanUseAgent())
            {
                return false;
            }

            if (agent.pathPending)
            {
                return false;
            }

            if (agent.remainingDistance > destinationReachedDistance)
            {
                return false;
            }

            return !agent.hasPath || agent.velocity.sqrMagnitude <= 0.01f;
        }

        public bool TryGetReachableNavMeshPosition(Vector3 desiredPosition, out Vector3 reachablePosition)
        {
            reachablePosition = Vector3.zero;

            if (!CanUseAgent())
            {
                return false;
            }

            if (!NavMesh.SamplePosition(
                    desiredPosition,
                    out NavMeshHit sampledHit,
                    navMeshSampleRadius,
                    NavMesh.AllAreas))
            {
                return false;
            }

            NavMeshPath path = new NavMeshPath();

            bool hasPath = NavMesh.CalculatePath(
                transform.position,
                sampledHit.position,
                NavMesh.AllAreas,
                path);

            if (!hasPath || path.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            reachablePosition = sampledHit.position;
            return true;
        }

        public bool TryGetRandomNavMeshPosition(Vector3 origin, float radius, out Vector3 position)
        {
            radius = Mathf.Max(0.1f, radius);

            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * radius;
                Vector3 randomPosition = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (NavMesh.SamplePosition(randomPosition, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                {
                    position = hit.position;
                    return true;
                }
            }

            position = origin;
            return false;
        }

        public void FacePosition(Vector3 position)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        public void FaceForwardFlat()
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude <= 0.001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private bool CanUseAgent()
        {
            return agent != null && agent.enabled && agent.isOnNavMesh;
        }
    }
}