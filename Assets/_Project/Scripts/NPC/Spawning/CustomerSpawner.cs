using System.Collections;
using Project.Managers;
using Project.NPC.Customer;
using Project.SlotMachines;
using Project.World;
using UnityEngine;
using UnityEngine.AI;

namespace Project.NPC.Spawning
{
    public sealed class CustomerSpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private CustomerController customerPrefab;

        [Header("Profiles")]
        [SerializeField] private CustomerProfileSO[] possibleProfiles;

        [Header("Scene References")]
        [SerializeField] private CustomerSpawnPoint spawnPoint;
        [SerializeField] private CasinoEntrance casinoEntrance;
        [SerializeField] private SlotMachineManager slotMachineManager;

        [Header("Spawn Settings")]
        [SerializeField, Min(0)] private int maxActiveCustomers = 5;
        [SerializeField, Min(0.1f)] private float spawnIntervalSeconds = 5f;

        private int activeCustomers;

        private void Start()
        {
            if (slotMachineManager == null)
            {
                slotMachineManager = FindFirstObjectByType<SlotMachineManager>();
            }

            if (!HasRequiredReferences())
            {
                enabled = false;
                return;
            }

            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            while (enabled)
            {
                if (activeCustomers < maxActiveCustomers)
                {
                    SpawnCustomer();
                }

                yield return new WaitForSeconds(spawnIntervalSeconds);
            }
        }

        private void SpawnCustomer()
        {
            CustomerProfileSO profile = GetRandomProfile();

            if (profile == null)
            {
                return;
            }

            CustomerController customer = Instantiate(
                customerPrefab,
                spawnPoint.Position,
                spawnPoint.Rotation);

            activeCustomers++;

            CustomerLifecycleReporter reporter = customer.gameObject.AddComponent<CustomerLifecycleReporter>();
            reporter.Initialize(() => activeCustomers = Mathf.Max(0, activeCustomers - 1));

            customer.Initialize(
                profile,
                casinoEntrance,
                slotMachineManager,
                spawnPoint.Position);
        }

        private CustomerProfileSO GetRandomProfile()
        {
            if (possibleProfiles == null || possibleProfiles.Length == 0)
            {
                return null;
            }

            return possibleProfiles[Random.Range(0, possibleProfiles.Length)];
        }

        private bool HasRequiredReferences()
        {
            bool isValid = true;

            if (customerPrefab == null)
            {
                Debug.LogError($"{nameof(CustomerSpawner)} is missing Customer Prefab.", this);
                isValid = false;
            }

            if (possibleProfiles == null || possibleProfiles.Length == 0)
            {
                Debug.LogError($"{nameof(CustomerSpawner)} has no possible customer profiles.", this);
                isValid = false;
            }

            if (spawnPoint == null)
            {
                Debug.LogError($"{nameof(CustomerSpawner)} is missing Customer Spawn Point.", this);
                isValid = false;
            }

            if (casinoEntrance == null)
            {
                Debug.LogError($"{nameof(CustomerSpawner)} is missing Casino Entrance.", this);
                isValid = false;
            }

            if (slotMachineManager == null)
            {
                Debug.LogError($"{nameof(CustomerSpawner)} is missing Slot Machine Manager.", this);
                isValid = false;
            }

            return isValid;
        }

        private sealed class CustomerLifecycleReporter : MonoBehaviour
        {
            private System.Action destroyed;

            public void Initialize(System.Action onDestroyed)
            {
                destroyed = onDestroyed;
            }

            private void OnDestroy()
            {
                destroyed?.Invoke();
            }
        }
    }
}