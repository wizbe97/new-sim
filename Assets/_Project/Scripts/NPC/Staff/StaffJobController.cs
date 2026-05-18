using System.Collections.Generic;
using Project.CashDesk;
using Project.Managers;
using Project.SlotMachines;
using UnityEngine;
using UnityEngine.AI;

namespace Project.Staff
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StaffMember))]
    public sealed class StaffJobController : MonoBehaviour
    {
        private enum SlotCollectionState
        {
            Searching,
            MovingToCollectionPoint,
            Collecting,
            WanderingBecauseNoWork
        }

        [Header("Current Job")]
        [SerializeField] private StaffJob currentJob = new();

        [Header("Idle")]
        [Tooltip("Where this staff member stands when idle. This should be assigned by StaffManager from a scene reference.")]
        [SerializeField] private Transform idleStandPoint;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float destinationReachedDistance = 0.7f;
        [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 2f;

        [Header("Slot Collection Points")]
        [Tooltip("How close the staff must be to a slot collection point before starting the collection timer.")]
        [SerializeField, Min(0.05f)] private float collectionPointReachedDistance = 0.75f;

        [Header("No Work Wandering")]
        [Tooltip("Staff only wander when no reachable slot machine needs collecting.")]
        [SerializeField] private bool wanderWhenNoSlotCollectionWork = true;

        [SerializeField, Min(0.5f)] private float noWorkWanderRadius = 8f;
        [SerializeField, Min(0.5f)] private float noWorkWanderIntervalSeconds = 4f;

        private readonly List<SlotMachine> skippedSlotMachines = new();

        private StaffMember staffMember;
        private NavMeshAgent agent;

        private SlotMachineManager slotMachineManager;
        private CashDeskManager cashDeskManager;

        private SlotMachine targetSlotMachine;
        private Transform targetCollectionPoint;
        private Vector3 targetCollectionPosition;

        private SlotCollectionState slotCollectionState = SlotCollectionState.Searching;

        private float slotCollectionCompleteTime;
        private float nextSlotCollectionSearchTime;
        private float nextNoWorkWanderTime;

        private CashOutTicket activeCashDeskTicket;
        private float activeCashDeskTicketProcessStartTime = -1f;

        private Vector3 idleFallbackPosition;
        private bool hasIdleFallbackPosition;

        private bool isInitialized;

        public StaffJob CurrentJob => currentJob;
        public StaffJobCategory CurrentJobCategory => currentJob != null ? currentJob.Category : StaffJobCategory.Idle;

        private void Awake()
        {
            staffMember = GetComponent<StaffMember>();
            agent = GetComponent<NavMeshAgent>();

            currentJob ??= new StaffJob();
        }

        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            if (staffMember == null || !staffMember.IsReadyForJobs)
            {
                return;
            }

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            switch (CurrentJobCategory)
            {
                case StaffJobCategory.Idle:
                    UpdateIdleJob();
                    break;

                case StaffJobCategory.SlotCollection:
                    UpdateSlotCollectionJob();
                    break;

                case StaffJobCategory.CashDesk:
                    UpdateCashDeskJob();
                    break;

                case StaffJobCategory.TableGames:
                case StaffJobCategory.Cleaning:
                case StaffJobCategory.Repairs:
                    UpdateIdleJob();
                    break;
            }
        }

        public void Initialize(
            StaffMember newStaffMember,
            SlotMachineManager newSlotMachineManager,
            CashDeskManager newCashDeskManager,
            Transform newIdleStandPoint)
        {
            staffMember = newStaffMember != null ? newStaffMember : GetComponent<StaffMember>();
            agent = staffMember != null ? staffMember.Agent : GetComponent<NavMeshAgent>();

            slotMachineManager = newSlotMachineManager;
            cashDeskManager = newCashDeskManager;
            idleStandPoint = newIdleStandPoint;

            if (staffMember == null)
            {
                Debug.LogError($"{nameof(StaffJobController)} cannot initialize because StaffMember is missing.", this);
                return;
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

            currentJob ??= new StaffJob();

            CaptureIdleFallbackPosition();

            ResetSlotCollectionState();
            ResetCashDeskTicketState();

            isInitialized = true;

            Debug.Log($"{nameof(StaffJobController)} initialized for {staffMember.StaffName}. Current job: {CurrentJobCategory}.", this);
        }

        public void SetIdleStandPoint(Transform newIdleStandPoint)
        {
            idleStandPoint = newIdleStandPoint;
        }

        public void AssignJob(StaffJobCategory category)
        {
            currentJob ??= new StaffJob();
            currentJob.SetCategory(category);

            ResetSlotCollectionState();
            ResetCashDeskTicketState();

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }

            Debug.Log($"{staffMember.StaffName} assigned to job: {category}.", this);
        }

        private void UpdateIdleJob()
        {
            Vector3 idlePosition = GetIdlePosition();

            if (HasReachedPosition(idlePosition))
            {
                agent.ResetPath();
                FaceForwardFlat();
                return;
            }

            SetDestination(idlePosition);
        }

        private void UpdateSlotCollectionJob()
        {
            switch (slotCollectionState)
            {
                case SlotCollectionState.Searching:
                    UpdateSlotCollectionSearching();
                    break;

                case SlotCollectionState.MovingToCollectionPoint:
                    UpdateSlotCollectionMovingToCollectionPoint();
                    break;

                case SlotCollectionState.Collecting:
                    UpdateSlotCollectionCollecting();
                    break;

                case SlotCollectionState.WanderingBecauseNoWork:
                    UpdateNoWorkWandering();
                    break;
            }
        }

        private void UpdateSlotCollectionSearching()
        {
            if (Time.time < nextSlotCollectionSearchTime)
            {
                return;
            }

            skippedSlotMachines.Clear();

            while (slotMachineManager.TryGetCollectableMachine(skippedSlotMachines, out SlotMachine candidateMachine))
            {
                if (TryAssignReachableSlotMachine(candidateMachine))
                {
                    return;
                }

                skippedSlotMachines.Add(candidateMachine);
            }

            BeginNoWorkWandering();
        }

        private bool TryAssignReachableSlotMachine(SlotMachine candidateMachine)
        {
            if (candidateMachine == null)
            {
                return false;
            }

            if (!TryFindReachableCollectionPoint(
                    candidateMachine,
                    out Transform collectionPoint,
                    out Vector3 navMeshPosition))
            {
                Debug.LogWarning(
                    $"{staffMember.StaffName} could not reach any staff collection point on {candidateMachine.name}. Trying another machine.",
                    this);

                return false;
            }

            targetSlotMachine = candidateMachine;
            targetCollectionPoint = collectionPoint;
            targetCollectionPosition = navMeshPosition;

            if (!SetDestination(targetCollectionPosition))
            {
                return false;
            }

            slotCollectionState = SlotCollectionState.MovingToCollectionPoint;

            Debug.Log(
                $"{staffMember.StaffName} is going to {candidateMachine.name} via collection point {collectionPoint.name}. Stored Cash: £{candidateMachine.StoredCashFromDeposits}.",
                this);

            return true;
        }

        private bool TryFindReachableCollectionPoint(
            SlotMachine slotMachine,
            out Transform collectionPoint,
            out Vector3 navMeshPosition)
        {
            collectionPoint = null;
            navMeshPosition = Vector3.zero;

            if (slotMachine == null)
            {
                return false;
            }

            if (!slotMachine.HasAnyStaffCollectionPoint())
            {
                Debug.LogWarning(
                    $"{slotMachine.name} has no staff collection points assigned in the SlotMachine inspector.",
                    slotMachine);

                return false;
            }

            for (int i = 0; i < slotMachine.StaffCollectionPoints.Count; i++)
            {
                Transform candidatePoint = slotMachine.StaffCollectionPoints[i];

                if (candidatePoint == null)
                {
                    continue;
                }

                if (!TryGetReachableNavMeshPosition(candidatePoint.position, out Vector3 reachablePosition))
                {
                    continue;
                }

                collectionPoint = candidatePoint;
                navMeshPosition = reachablePosition;
                return true;
            }

            return false;
        }

        private bool TryGetReachableNavMeshPosition(Vector3 desiredPosition, out Vector3 reachablePosition)
        {
            reachablePosition = Vector3.zero;

            if (!NavMesh.SamplePosition(
                    desiredPosition,
                    out NavMeshHit sampledHit,
                    navMeshSampleRadius,
                    NavMesh.AllAreas))
            {
                return false;
            }

            NavMeshPath path = new NavMeshPath();

            bool pathCalculated = NavMesh.CalculatePath(
                transform.position,
                sampledHit.position,
                NavMesh.AllAreas,
                path);

            if (!pathCalculated || path.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            reachablePosition = sampledHit.position;
            return true;
        }

        private void UpdateSlotCollectionMovingToCollectionPoint()
        {
            if (!IsTargetSlotMachineStillValid())
            {
                ResetSlotCollectionState();
                return;
            }

            if (!HasReachedPosition(targetCollectionPosition, collectionPointReachedDistance) &&
                !HasAgentReachedDestination())
            {
                return;
            }

            BeginSlotCollectionTimer();
        }

        private void BeginSlotCollectionTimer()
        {
            agent.ResetPath();
            FacePosition(targetSlotMachine.transform.position);

            float duration = Mathf.Max(0f, staffMember.SlotCollectionSpeed);
            slotCollectionCompleteTime = Time.time + duration;

            slotCollectionState = SlotCollectionState.Collecting;

            Debug.Log(
                $"{staffMember.StaffName} started collecting cash from {targetSlotMachine.name}. Duration: {duration:0.00}s.",
                this);
        }

        private void UpdateSlotCollectionCollecting()
        {
            if (!IsTargetSlotMachineStillValid())
            {
                ResetSlotCollectionState();
                return;
            }

            agent.ResetPath();
            FacePosition(targetSlotMachine.transform.position);

            if (Time.time < slotCollectionCompleteTime)
            {
                return;
            }

            int requestedAmount = currentJob.GetSlotCollectionRequestAmount(
                targetSlotMachine.StoredCashFromDeposits);

            bool collected = slotMachineManager.TryCollectSlotCash(
                targetSlotMachine,
                requestedAmount,
                out int collectedAmount,
                staffMember);

            if (collected)
            {
                Debug.Log($"{staffMember.StaffName} collected £{collectedAmount} from {targetSlotMachine.name}.", this);
            }
            else
            {
                Debug.LogWarning(
                    $"{staffMember.StaffName} reached {targetSlotMachine.name}, but collection failed. " +
                    $"Requested: £{requestedAmount}, Stored Cash: £{targetSlotMachine.StoredCashFromDeposits}.",
                    this);
            }

            ResetSlotCollectionState();
        }

        private bool IsTargetSlotMachineStillValid()
        {
            if (targetSlotMachine == null)
            {
                return false;
            }

            if (targetSlotMachine.IsBeingMovedForPlacement)
            {
                return false;
            }

            return targetSlotMachine.StoredCashFromDeposits > 0;
        }

        private void BeginNoWorkWandering()
        {
            ResetSlotCollectionTargets();

            nextSlotCollectionSearchTime =
                Time.time + currentJob.SlotCollectionSearchIntervalSeconds;

            nextNoWorkWanderTime = 0f;

            slotCollectionState = SlotCollectionState.WanderingBecauseNoWork;

            Debug.Log($"{staffMember.StaffName} found no reachable slot machines with cash to collect.", this);
        }

        private void UpdateNoWorkWandering()
        {
            if (Time.time >= nextSlotCollectionSearchTime)
            {
                slotCollectionState = SlotCollectionState.Searching;
                return;
            }

            if (!wanderWhenNoSlotCollectionWork)
            {
                return;
            }

            if (Time.time < nextNoWorkWanderTime)
            {
                return;
            }

            if (TryGetRandomNavMeshPosition(transform.position, noWorkWanderRadius, out Vector3 wanderPosition))
            {
                SetDestination(wanderPosition);
            }

            nextNoWorkWanderTime = Time.time + noWorkWanderIntervalSeconds;
        }

        private void ResetSlotCollectionState()
        {
            ResetSlotCollectionTargets();

            skippedSlotMachines.Clear();
            slotCollectionCompleteTime = 0f;
            nextSlotCollectionSearchTime = Time.time;
            nextNoWorkWanderTime = 0f;
            slotCollectionState = SlotCollectionState.Searching;
        }

        private void ResetSlotCollectionTargets()
        {
            targetSlotMachine = null;
            targetCollectionPoint = null;
            targetCollectionPosition = transform.position;
        }

        private void UpdateCashDeskJob()
        {
            if (!cashDeskManager.TryGetStaffServicePosition(out Vector3 servicePosition, out Vector3 facingPosition))
            {
                ResetCashDeskTicketState();
                return;
            }

            if (!HasReachedPosition(servicePosition))
            {
                ResetCashDeskTicketState();
                SetDestination(servicePosition);
                return;
            }

            agent.ResetPath();
            FacePosition(facingPosition);

            if (!cashDeskManager.TryGetFrontTicket(out CashOutTicket frontTicket))
            {
                ResetCashDeskTicketState();
                return;
            }

            if (!frontTicket.IsPlacedOnDesk || frontTicket.IsPaid)
            {
                ResetCashDeskTicketState();
                return;
            }

            if (activeCashDeskTicket != frontTicket)
            {
                activeCashDeskTicket = frontTicket;
                activeCashDeskTicketProcessStartTime = Time.time;

                Debug.Log(
                    $"{staffMember.StaffName} started processing ticket {frontTicket.name} at the cash desk. " +
                    $"Ticket placed at {frontTicket.PlacedOnDeskTime:0.00}, processing started at {activeCashDeskTicketProcessStartTime:0.00}.",
                    this);
            }

            float earliestPayoutTime =
                activeCashDeskTicketProcessStartTime + currentJob.CashDeskPayoutIntervalSeconds;

            if (Time.time < earliestPayoutTime)
            {
                return;
            }

            bool paid = cashDeskManager.TryPayFrontTicket(staffMember);

            if (paid)
            {
                Debug.Log($"{staffMember.StaffName} paid the front cash desk ticket.", this);
                ResetCashDeskTicketState();
            }
        }

        private void ResetCashDeskTicketState()
        {
            activeCashDeskTicket = null;
            activeCashDeskTicketProcessStartTime = -1f;
        }

        private Vector3 GetIdlePosition()
        {
            if (idleStandPoint != null)
            {
                return idleStandPoint.position;
            }

            if (hasIdleFallbackPosition)
            {
                return idleFallbackPosition;
            }

            return transform.position;
        }

        private void CaptureIdleFallbackPosition()
        {
            idleFallbackPosition = transform.position;
            hasIdleFallbackPosition = true;
        }

        private bool SetDestination(Vector3 destination)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return false;
            }

            if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                return false;
            }

            agent.isStopped = false;
            agent.SetDestination(hit.position);

            return true;
        }

        private bool HasReachedPosition(Vector3 position)
        {
            return HasReachedPosition(position, destinationReachedDistance);
        }

        private bool HasReachedPosition(Vector3 position, float reachDistance)
        {
            float distanceSquared = (transform.position - position).sqrMagnitude;
            return distanceSquared <= reachDistance * reachDistance;
        }

        private bool HasAgentReachedDestination()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return false;
            }

            if (agent.pathPending)
            {
                return false;
            }

            if (agent.remainingDistance > Mathf.Max(agent.stoppingDistance, destinationReachedDistance))
            {
                return false;
            }

            return !agent.hasPath || agent.velocity.sqrMagnitude <= 0.01f;
        }

        private bool TryGetRandomNavMeshPosition(Vector3 origin, float radius, out Vector3 position)
        {
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

        private void FacePosition(Vector3 position)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void FaceForwardFlat()
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude <= 0.001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            currentJob ??= new StaffJob();
            currentJob.Validate();

            destinationReachedDistance = Mathf.Max(0.1f, destinationReachedDistance);
            navMeshSampleRadius = Mathf.Max(0.1f, navMeshSampleRadius);

            collectionPointReachedDistance = Mathf.Max(0.05f, collectionPointReachedDistance);

            noWorkWanderRadius = Mathf.Max(0.5f, noWorkWanderRadius);
            noWorkWanderIntervalSeconds = Mathf.Max(0.5f, noWorkWanderIntervalSeconds);
        }
#endif
    }
}