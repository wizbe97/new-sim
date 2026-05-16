using System;
using System.Collections;
using Project.World;
using UnityEngine;
using UnityEngine.AI;

namespace Project.NPC.Customer
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CustomerMovement : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField, Min(0.05f)] private float destinationReachedDistance = 0.35f;

        [Header("Debug Logging")]
        [SerializeField] private bool logIdleBehaviour = true;

        private NavMeshAgent agent;
        private CasinoRoamPoint[] roamPoints;
        private Action<CustomerState> setState;

        public float DestinationReachedDistance => destinationReachedDistance;
        public bool LogIdleBehaviour => logIdleBehaviour;

        public void Initialize(Action<CustomerState> setCustomerState)
        {
            setState = setCustomerState;

            agent = GetComponent<NavMeshAgent>();
            roamPoints = FindObjectsByType<CasinoRoamPoint>(FindObjectsSortMode.None);
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

            if (shouldStandStill || roamPoints == null || roamPoints.Length == 0)
            {
                setState?.Invoke(CustomerState.StandingIdle);

                if (logIdleBehaviour)
                {
                    Debug.Log($"{name} is standing idle inside the casino.", this);
                }

                yield return new WaitForSeconds(profile.GetRandomIdleWaitSeconds());
                yield break;
            }

            CasinoRoamPoint roamPoint = GetRandomRoamPoint();

            if (roamPoint == null)
            {
                yield return new WaitForSeconds(profile.GetRandomIdleWaitSeconds());
                yield break;
            }

            setState?.Invoke(CustomerState.Wandering);

            if (logIdleBehaviour)
            {
                Debug.Log($"{name} is wandering to {roamPoint.name}.", roamPoint);
            }

            yield return MoveTo(roamPoint.Position);

            setState?.Invoke(CustomerState.StandingIdle);

            yield return new WaitForSeconds(profile.GetRandomIdleWaitSeconds());
        }

        private CasinoRoamPoint GetRandomRoamPoint()
        {
            if (roamPoints == null || roamPoints.Length == 0)
            {
                return null;
            }

            return roamPoints[UnityEngine.Random.Range(0, roamPoints.Length)];
        }
    }
}