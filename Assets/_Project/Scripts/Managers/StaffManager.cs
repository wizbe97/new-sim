using System;
using System.Collections.Generic;
using Project.Staff;
using UnityEngine;

namespace Project.Managers
{
    public sealed class StaffManager : MonoBehaviour
    {
        [Header("Delivery Location")]
        [Tooltip("Staff will fall from the sky above this point. Assign the same transform used by the item delivery pad.")]
        [SerializeField] private Transform staffDeliveryPad;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnHeight = 20f;
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;
        [SerializeField] private bool randomizeHorizontalOffset = true;
        [SerializeField, Min(0f)] private float randomHorizontalRadius = 0.4f;

        [Header("Physics Settings")]
        [SerializeField] private bool ensureRigidbody = true;
        [SerializeField] private bool ensureCollider = true;
        [SerializeField] private float staffMass = 3f;
        [SerializeField] private float staffDrag = 0.1f;
        [SerializeField] private float staffAngularDrag = 0.05f;
        [SerializeField] private Vector3 randomTorqueRange = new Vector3(3f, 3f, 3f);

        private readonly List<StaffMember> hiredStaff = new();
        private readonly HashSet<string> hiredUniqueStaffIds = new();

        private PlayerBalanceManager playerBalanceManager;
        private CasinoProgressionManager casinoProgressionManager;
        private UIManager uiManager;

        public IReadOnlyList<StaffMember> HiredStaff => hiredStaff;
        public bool HasStaffDeliveryPad => staffDeliveryPad != null;

        public event Action StaffChanged;

        public void Initialize(
            PlayerBalanceManager balanceManager,
            CasinoProgressionManager progressionManager,
            UIManager newUIManager)
        {
            playerBalanceManager = balanceManager;
            casinoProgressionManager = progressionManager;
            uiManager = newUIManager;

            if (playerBalanceManager == null)
            {
                Debug.LogError($"{nameof(StaffManager)} cannot initialize because PlayerBalanceManager is missing.", this);
                return;
            }

            if (casinoProgressionManager == null)
            {
                Debug.LogError($"{nameof(StaffManager)} cannot initialize because CasinoProgressionManager is missing.", this);
                return;
            }

            if (uiManager == null)
            {
                Debug.LogError($"{nameof(StaffManager)} cannot initialize because UIManager is missing.", this);
                return;
            }

            uiManager.StaffHireRequested += HandleStaffHireRequested;
            uiManager.SetStaffOwnershipProvider(IsStaffMemberHired);
            uiManager.SetStaffUnlockProvider(IsStaffMemberUnlocked);

            casinoProgressionManager.LevelChanged += HandleCasinoLevelChanged;

            Debug.Log($"{nameof(StaffManager)} initialized.", this);
        }

        private void OnDestroy()
        {
            if (uiManager != null)
            {
                uiManager.StaffHireRequested -= HandleStaffHireRequested;
            }

            if (casinoProgressionManager != null)
            {
                casinoProgressionManager.LevelChanged -= HandleCasinoLevelChanged;
            }
        }

        public void InitializeDeliveryPad(Transform deliveryPadTransform)
        {
            if (deliveryPadTransform == null)
            {
                Debug.LogError($"{nameof(StaffManager)} cannot initialize staff delivery pad because transform is missing.", this);
                ClearDeliveryPad();
                return;
            }

            staffDeliveryPad = deliveryPadTransform;
        }

        public void ClearDeliveryPad()
        {
            staffDeliveryPad = null;
        }

        public bool IsStaffMemberUnlocked(StaffMemberSO staffMember)
        {
            if (staffMember == null)
            {
                return false;
            }

            if (casinoProgressionManager == null)
            {
                return false;
            }

            return casinoProgressionManager.CanUseLevel(staffMember.RequiredCasinoLevel);
        }

        public bool IsStaffMemberHired(StaffMemberSO staffMember)
        {
            if (staffMember == null ||
                !staffMember.IsUniqueHire ||
                string.IsNullOrWhiteSpace(staffMember.StaffId))
            {
                return false;
            }

            return hiredUniqueStaffIds.Contains(staffMember.StaffId);
        }

        public bool TryHireStaffMember(StaffMemberSO staffMember)
        {
            if (staffMember == null)
            {
                Debug.LogError($"{nameof(StaffManager)} cannot hire staff because StaffMemberSO is missing.", this);
                return false;
            }

            if (staffMember.Prefab == null)
            {
                Debug.LogError($"{nameof(StaffManager)} cannot hire {staffMember.StaffName} because Prefab is missing.", staffMember);
                return false;
            }

            if (staffDeliveryPad == null)
            {
                Debug.LogWarning($"{nameof(StaffManager)} cannot hire {staffMember.StaffName} because Staff Delivery Pad is missing.", this);
                return false;
            }

            if (!IsStaffMemberUnlocked(staffMember))
            {
                Debug.LogWarning(
                    $"{staffMember.StaffName} is locked. Requires casino level {staffMember.RequiredCasinoLevel}.",
                    staffMember);

                return false;
            }

            if (staffMember.IsUniqueHire && IsStaffMemberHired(staffMember))
            {
                Debug.LogWarning($"{staffMember.StaffName} is unique and has already been hired.", staffMember);
                return false;
            }

            if (!playerBalanceManager.CanAfford(staffMember.UnlockCost))
            {
                Debug.LogWarning(
                    $"Not enough money to hire {staffMember.StaffName}. " +
                    $"Cost: £{staffMember.UnlockCost:N0}, Balance: £{playerBalanceManager.CurrentBalance:N0}.",
                    staffMember);

                return false;
            }

            if (!playerBalanceManager.TrySpend(staffMember.UnlockCost))
            {
                Debug.LogWarning($"Hire payment failed for {staffMember.StaffName}.", staffMember);
                return false;
            }

            StaffMember spawnedStaff = SpawnStaffMember(staffMember);

            if (spawnedStaff == null)
            {
                playerBalanceManager.AddBalance(staffMember.UnlockCost);

                Debug.LogError(
                    $"{staffMember.StaffName} hire was refunded because spawning failed.",
                    staffMember);

                return false;
            }

            RegisterHiredStaff(spawnedStaff, staffMember);

            Debug.Log(
                $"Hired staff member: {staffMember.StaffName}. Daily Salary: £{staffMember.DailySalary}.",
                spawnedStaff);

            StaffChanged?.Invoke();
            uiManager.RefreshPhoneStaffItems();

            return true;
        }

        private void HandleStaffHireRequested(StaffMemberSO staffMember)
        {
            TryHireStaffMember(staffMember);
        }

        private StaffMember SpawnStaffMember(StaffMemberSO staffMember)
        {
            Vector3 spawnPosition = GetSpawnPosition();
            Quaternion spawnRotation = Quaternion.identity;

            StaffMember staffInstance = Instantiate(
                staffMember.Prefab,
                spawnPosition,
                spawnRotation);

            staffInstance.name = $"Staff_{staffMember.StaffName}";
            staffInstance.Initialize(staffMember);

            ConfigurePhysics(staffInstance.gameObject);
            ApplyDeliveryGimmick(staffInstance.GetComponent<Rigidbody>());

            staffInstance.BeginSkyDrop();

            return staffInstance;
        }

        private Vector3 GetSpawnPosition()
        {
            Vector3 basePosition = staffDeliveryPad.position;
            Vector3 finalOffset = spawnOffset;

            if (randomizeHorizontalOffset)
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * randomHorizontalRadius;
                finalOffset.x += randomCircle.x;
                finalOffset.z += randomCircle.y;
            }

            return new Vector3(
                basePosition.x + finalOffset.x,
                spawnHeight + finalOffset.y,
                basePosition.z + finalOffset.z);
        }

        private Rigidbody ConfigurePhysics(GameObject staffObject)
        {
            Rigidbody rigidbody = staffObject.GetComponent<Rigidbody>();

            if (rigidbody == null && ensureRigidbody)
            {
                rigidbody = staffObject.AddComponent<Rigidbody>();
            }

            if (rigidbody != null)
            {
                rigidbody.mass = staffMass;
                rigidbody.drag = staffDrag;
                rigidbody.angularDrag = staffAngularDrag;
                rigidbody.useGravity = true;
                rigidbody.isKinematic = false;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            }

            Collider collider = staffObject.GetComponent<Collider>();

            if (collider == null && ensureCollider)
            {
                staffObject.AddComponent<CapsuleCollider>();
            }

            return rigidbody;
        }

        private void ApplyDeliveryGimmick(Rigidbody rigidbody)
        {
            if (rigidbody == null)
            {
                return;
            }

            Vector3 randomTorque = new Vector3(
                UnityEngine.Random.Range(-randomTorqueRange.x, randomTorqueRange.x),
                UnityEngine.Random.Range(-randomTorqueRange.y, randomTorqueRange.y),
                UnityEngine.Random.Range(-randomTorqueRange.z, randomTorqueRange.z));

            rigidbody.AddTorque(randomTorque, ForceMode.Impulse);
        }

        private void RegisterHiredStaff(StaffMember staffInstance, StaffMemberSO staffConfig)
        {
            if (staffInstance == null || staffConfig == null)
            {
                return;
            }

            hiredStaff.Add(staffInstance);

            if (staffConfig.IsUniqueHire && !string.IsNullOrWhiteSpace(staffConfig.StaffId))
            {
                hiredUniqueStaffIds.Add(staffConfig.StaffId);
            }
        }

        private void HandleCasinoLevelChanged(int newLevel)
        {
            uiManager.RefreshPhoneStaffItems();
        }
    }
}