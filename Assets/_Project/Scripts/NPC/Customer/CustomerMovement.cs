using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Project.NPC.Customer
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CustomerMovement : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField, Min(0.05f)] private float destinationReachedDistance = 0.35f;

        [Header("Random Exploration")]
        [Tooltip("How far from the customer's current position they can choose a random exploration destination.")]
        [SerializeField, Min(0.5f)] private float explorationRadius = 6f;

        [Tooltip("Minimum distance a random exploration point should be from the customer's current position.")]
        [SerializeField, Min(0.1f)] private float minimumExplorationDistance = 1.5f;

        [Tooltip("How many random NavMesh positions the customer may visit during one waiting/exploration action.")]
        [SerializeField, Min(1)] private int minExplorationSteps = 1;

        [Tooltip("How many random NavMesh positions the customer may visit during one waiting/exploration action.")]
        [SerializeField, Min(1)] private int maxExplorationSteps = 3;

        [Tooltip("How many attempts are made to find a valid NavMesh point before giving up and standing idle.")]
        [SerializeField, Min(1)] private int maxSampleAttempts = 12;

        [Tooltip("How long the customer pauses between small exploration movements.")]
        [SerializeField, Min(0f)] private float minPauseBetweenExplorationSteps = 0.25f;

        [Tooltip("How long the customer pauses between small exploration movements.")]
        [SerializeField, Min(0f)] private float maxPauseBetweenExplorationSteps = 1.25f;

        [Header("Debug Logging")]
        [SerializeField] private bool logIdleBehaviour = true;

        private NavMeshAgent agent;
        private Action<CustomerState> setState;

        public float DestinationReachedDistance => destinationReachedDistance;
        public bool LogIdleBehaviour => logIdleBehaviour;

        public void Initialize(Action<CustomerState> setCustomerState)
        {
            setState = setCustomerState;
            agent = GetComponent<NavMeshAgent>();
        }

        public IEnumerator MoveTo(Vector3 destination)
        {
            if (agent == null)
            {
                yield break;
            }

            bool destinationSet = agent.SetDestination(destination);

            if (!destinationSet)
            {
                Debug.LogWarning($"{name} could not path to destination {destination}.", this);
                yield break;
            }

            while (agent.pathPending)
            {
                yield return null;
            }

            while (agent.enabled && agent.remainingDistance > destinationReachedDistance)
            {
                yield return null;
            }
        }

        public bool SetDestination(Vector3 destination)
        {
            if (agent == null || !agent.enabled)
            {
                return false;
            }

            return agent.SetDestination(destination);
        }

        public bool HasReachedDestination()
        {
            if (agent == null || agent.pathPending)
            {
                return false;
            }

            return agent.remainingDistance <= destinationReachedDistance;
        }

        public void BeginImmediateMove(Vector3 destination)
        {
            if (agent == null || !agent.enabled)
            {
                return;
            }

            agent.ResetPath();
            agent.SetDestination(destination);
        }

        public void FacePosition(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        public IEnumerator WaitInsideCasino(CustomerProfileSO profile)
        {
            if (profile == null)
            {
                yield break;
            }

            bool shouldStandStill = UnityEngine.Random.value < profile.StandStillWhileWaitingChance;

            if (shouldStandStill)
            {
                yield return StandIdle(profile);
                yield break;
            }

            yield return ExploreRandomly(profile);
        }

        private IEnumerator StandIdle(CustomerProfileSO profile)
        {
            setState?.Invoke(CustomerState.StandingIdle);

            if (logIdleBehaviour)
            {
                Debug.Log($"{name} is standing idle inside the casino.", this);
            }

            yield return new WaitForSeconds(profile.GetRandomIdleWaitSeconds());
        }

        private IEnumerator ExploreRandomly(CustomerProfileSO profile)
        {
            int minSteps = Mathf.Min(minExplorationSteps, maxExplorationSteps);
            int maxSteps = Mathf.Max(minExplorationSteps, maxExplorationSteps);
            int steps = UnityEngine.Random.Range(minSteps, maxSteps + 1);

            bool walkedSomewhere = false;

            for (int i = 0; i < steps; i++)
            {
                if (!TryGetRandomReachablePoint(out Vector3 destination))
                {
                    continue;
                }

                walkedSomewhere = true;

                setState?.Invoke(CustomerState.Wandering);

                if (logIdleBehaviour)
                {
                    Debug.Log($"{name} is exploring the casino.", this);
                }

                yield return MoveTo(destination);

                setState?.Invoke(CustomerState.StandingIdle);

                float pause = UnityEngine.Random.Range(
                    Mathf.Min(minPauseBetweenExplorationSteps, maxPauseBetweenExplorationSteps),
                    Mathf.Max(minPauseBetweenExplorationSteps, maxPauseBetweenExplorationSteps));

                if (pause > 0f)
                {
                    yield return new WaitForSeconds(pause);
                }
            }

            if (!walkedSomewhere)
            {
                yield return StandIdle(profile);
                yield break;
            }

            yield return new WaitForSeconds(profile.GetRandomIdleWaitSeconds());
        }

        private bool TryGetRandomReachablePoint(out Vector3 point)
        {
            point = transform.position;

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return false;
            }

            for (int i = 0; i < maxSampleAttempts; i++)
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * explorationRadius;

                Vector3 candidate = transform.position + new Vector3(
                    randomCircle.x,
                    0f,
                    randomCircle.y);

                if ((candidate - transform.position).sqrMagnitude < minimumExplorationDistance * minimumExplorationDistance)
                {
                    continue;
                }

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, explorationRadius, NavMesh.AllAreas))
                {
                    continue;
                }

                if (!HasCompletePath(hit.position))
                {
                    continue;
                }

                point = hit.position;
                return true;
            }

            return false;
        }

        private bool HasCompletePath(Vector3 destination)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return false;
            }

            NavMeshPath path = new NavMeshPath();

            if (!agent.CalculatePath(destination, path))
            {
                return false;
            }

            return path.status == NavMeshPathStatus.PathComplete;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (maxExplorationSteps < minExplorationSteps)
            {
                maxExplorationSteps = minExplorationSteps;
            }

            if (maxPauseBetweenExplorationSteps < minPauseBetweenExplorationSteps)
            {
                maxPauseBetweenExplorationSteps = minPauseBetweenExplorationSteps;
            }

            minimumExplorationDistance = Mathf.Min(minimumExplorationDistance, explorationRadius);
        }
#endif
    }
}