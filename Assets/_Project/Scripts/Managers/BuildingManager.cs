using Project.Player;
using UnityEngine;

namespace Project.Placement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FurniturePlacementPoseProvider))]
    [RequireComponent(typeof(FurniturePlacementValidator))]
    [RequireComponent(typeof(FurniturePlacementPreviewController))]
    public sealed class BuildingManager : MonoBehaviour
    {
        [Header("Rotation")]
        [SerializeField] private float rotationStepDegrees = 15f;

        private FirstPersonController player;
        private FurniturePlacementPoseProvider poseProvider;
        private FurniturePlacementValidator placementValidator;
        private FurniturePlacementPreviewController previewController;

        private FurnitureItem currentFurniture;

        private Vector3 originalPosition;
        private Quaternion originalRotation;

        private Vector3 targetPosition;
        private Quaternion targetRotation;

        private float yawOffset;
        private int startedFrame = -1;
        private bool canPlace;

        public bool IsBuilding => currentFurniture != null;
        public bool CanPlace => IsBuilding && canPlace;
        public FurnitureItem CurrentFurniture => currentFurniture;

        private void Awake()
        {
            CacheComponents();
        }

        public void Initialize(FirstPersonController newPlayer)
        {
            if (newPlayer == null)
            {
                Debug.LogError($"{nameof(BuildingManager)} cannot initialize without a player.", this);
                return;
            }

            player = newPlayer;

            CacheComponents();

            poseProvider.Initialize(player);
        }

        private void Update()
        {
            if (!IsBuilding)
            {
                return;
            }

            UpdatePreview();
        }

        public bool BeginBuilding(FurnitureItem furniture)
        {
            if (furniture == null)
            {
                Debug.LogWarning($"{nameof(BuildingManager)} tried to place null furniture.", this);
                return false;
            }

            if (IsBuilding)
            {
                Debug.LogWarning($"{nameof(BuildingManager)} is already placing furniture.", this);
                return false;
            }

            if (player == null || player.CameraTarget == null)
            {
                Debug.LogError($"{nameof(BuildingManager)} is missing player or CameraTarget.", this);
                return false;
            }

            if (furniture.PlacementBounds == null)
            {
                Debug.LogError($"{furniture.name} cannot be placed because it has no Placement Bounds assigned.", furniture);
                return false;
            }

            CacheComponents();

            currentFurniture = furniture;

            originalPosition = furniture.transform.position;
            originalRotation = furniture.transform.rotation;

            targetPosition = originalPosition;
            targetRotation = originalRotation;

            yawOffset = 0f;
            startedFrame = Time.frameCount;
            canPlace = false;

            poseProvider.BeginPlacement(
                currentFurniture,
                originalRotation.eulerAngles.y,
                player.CameraTarget.forward.y);

            previewController.BeginPreview(currentFurniture);

            UpdatePreview(immediate: true);

            return true;
        }

        public bool TryPlaceCurrentFurniture()
        {
            if (!IsBuilding)
            {
                return false;
            }

            if (Time.frameCount == startedFrame)
            {
                return false;
            }

            if (!canPlace)
            {
                Debug.Log("Cannot place furniture here.", currentFurniture);
                return false;
            }

            FurnitureItem placedFurniture = currentFurniture;

            placedFurniture.transform.SetPositionAndRotation(targetPosition, targetRotation);
            previewController.EndPreview(placedFurniture);

            ClearState();

            return true;
        }

        public void CancelCurrentFurniture()
        {
            if (!IsBuilding)
            {
                return;
            }

            FurnitureItem cancelledFurniture = currentFurniture;

            cancelledFurniture.transform.SetPositionAndRotation(originalPosition, originalRotation);
            previewController.EndPreview(cancelledFurniture);

            ClearState();
        }

        public void RotateCurrentFurniture()
        {
            if (!IsBuilding)
            {
                return;
            }

            yawOffset = Mathf.Repeat(yawOffset + rotationStepDegrees, 360f);
            UpdatePreview(immediate: true);
        }

        public FurnitureItem TakeCurrentFurnitureForBoxing()
        {
            if (!IsBuilding)
            {
                return null;
            }

            FurnitureItem furniture = currentFurniture;

            previewController.EndPreview(furniture);
            ClearState();

            return furniture;
        }

        private void UpdatePreview(bool immediate = false)
        {
            if (!poseProvider.TryGetPlacementPose(
                    currentFurniture,
                    yawOffset,
                    out targetPosition,
                    out targetRotation))
            {
                canPlace = false;
                previewController.ApplyInvalidPreview(currentFurniture);
                return;
            }

            canPlace = placementValidator.CanPlace(
                currentFurniture,
                targetPosition,
                targetRotation);

            previewController.UpdatePreview(
                currentFurniture,
                targetPosition,
                targetRotation,
                canPlace,
                immediate);
        }

        private void CacheComponents()
        {
            if (poseProvider == null)
            {
                poseProvider = GetComponent<FurniturePlacementPoseProvider>();

                if (poseProvider == null)
                {
                    poseProvider = gameObject.AddComponent<FurniturePlacementPoseProvider>();
                }
            }

            if (placementValidator == null)
            {
                placementValidator = GetComponent<FurniturePlacementValidator>();

                if (placementValidator == null)
                {
                    placementValidator = gameObject.AddComponent<FurniturePlacementValidator>();
                }
            }

            if (previewController == null)
            {
                previewController = GetComponent<FurniturePlacementPreviewController>();

                if (previewController == null)
                {
                    previewController = gameObject.AddComponent<FurniturePlacementPreviewController>();
                }
            }
        }

        private void ClearState()
        {
            currentFurniture = null;

            originalPosition = Vector3.zero;
            originalRotation = Quaternion.identity;

            targetPosition = Vector3.zero;
            targetRotation = Quaternion.identity;

            yawOffset = 0f;
            startedFrame = -1;
            canPlace = false;

            poseProvider.Clear();
            previewController.Clear();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            rotationStepDegrees = Mathf.Max(0f, rotationStepDegrees);
        }
#endif
    }
}