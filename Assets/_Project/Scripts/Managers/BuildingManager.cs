using Project.Player;
using UnityEngine;

namespace Project.Placement
{
    public sealed class BuildingManager : MonoBehaviour
    {
        [Header("Preview Distance")]
        [Tooltip("Base distance from the player/camera that the preview appears.")]
        [SerializeField, Min(0.1f)] private float placementDistance = 2.25f;

        [Tooltip("How strongly looking up/down moves the preview forward/backward.")]
        [SerializeField, Min(0f)] private float lookPitchDistanceInfluence = 2f;

        [Tooltip("Maximum extra distance added when looking up.")]
        [SerializeField, Min(0f)] private float maxLookForwardOffset = 1.5f;

        [Tooltip("Maximum distance removed when looking down.")]
        [SerializeField, Min(0f)] private float maxLookBackwardOffset = 1f;

        [Header("Preview Smoothing")]
        [SerializeField, Min(0f)] private float positionSmoothTime = 0.04f;
        [SerializeField, Min(0f)] private float rotationSmoothSpeed = 18f;

        [Header("Rotation")]
        [SerializeField] private float rotationStepDegrees = 15f;

        [Header("Surface Detection")]
        [SerializeField] private LayerMask placementSurfaceLayerMask = ~0;
        [SerializeField] private QueryTriggerInteraction surfaceTriggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField, Range(0f, 1f)] private float minimumSurfaceUpDot = 0.7f;
        [SerializeField, Min(0.1f)] private float rayStartHeight = 3f;
        [SerializeField, Min(0.1f)] private float rayDistance = 8f;

        [Header("Obstruction Detection")]
        [Tooltip("Exclude Player and PlacementSurface from this mask.")]
        [SerializeField] private LayerMask obstructionLayerMask = ~0;
        [SerializeField] private QueryTriggerInteraction obstructionTriggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField, Range(0f, 0.2f)] private float obstructionInset = 0.02f;

        [Header("Preview Materials")]
        [SerializeField] private Material validPreviewMaterial;
        [SerializeField] private Material invalidPreviewMaterial;

        [Header("Debug")]
        [SerializeField] private bool drawDebugRay = true;

        private FirstPersonController player;
        private FurnitureItem currentFurniture;

        private Vector3 originalPosition;
        private Quaternion originalRotation;

        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private Vector3 positionVelocity;

        private float startingYaw;
        private float yawOffset;
        private float pickupCameraForwardY;

        private int startedFrame = -1;
        private bool canPlace;

        public bool IsBuilding => currentFurniture != null;
        public bool CanPlace => IsBuilding && canPlace;
        public FurnitureItem CurrentFurniture => currentFurniture;

        public void Initialize(FirstPersonController newPlayer)
        {
            if (newPlayer == null)
            {
                Debug.LogError($"{nameof(BuildingManager)} cannot initialize without a player.", this);
                return;
            }

            player = newPlayer;
        }

        private void Update()
        {
            if (IsBuilding)
            {
                UpdatePreview();
            }
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

            currentFurniture = furniture;

            originalPosition = furniture.transform.position;
            originalRotation = furniture.transform.rotation;

            startingYaw = originalRotation.eulerAngles.y;
            yawOffset = 0f;
            pickupCameraForwardY = player.CameraTarget.forward.y;

            targetPosition = originalPosition;
            targetRotation = originalRotation;
            positionVelocity = Vector3.zero;

            startedFrame = Time.frameCount;
            canPlace = false;

            currentFurniture.BeginPreview();
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
            placedFurniture.EndPreview();

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
            cancelledFurniture.EndPreview();

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

            furniture.EndPreview();
            ClearState();

            return furniture;
        }

        private void UpdatePreview(bool immediate = false)
        {
            if (!TryGetPlacementPose(out targetPosition, out targetRotation))
            {
                canPlace = false;
                currentFurniture.SetPreviewMaterial(invalidPreviewMaterial);
                return;
            }

            canPlace = !IsBlocked(targetPosition, targetRotation);

            if (immediate || positionSmoothTime <= 0f)
            {
                currentFurniture.transform.SetPositionAndRotation(targetPosition, targetRotation);
            }
            else
            {
                currentFurniture.transform.position = Vector3.SmoothDamp(
                    currentFurniture.transform.position,
                    targetPosition,
                    ref positionVelocity,
                    positionSmoothTime
                );

                currentFurniture.transform.rotation = Quaternion.Slerp(
                    currentFurniture.transform.rotation,
                    targetRotation,
                    rotationSmoothSpeed * Time.deltaTime
                );
            }

            currentFurniture.SetPreviewMaterial(
                canPlace ? validPreviewMaterial : invalidPreviewMaterial
            );
        }

        private bool TryGetPlacementPose(out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = GetTargetRotation();

            Vector3 forward = Vector3.ProjectOnPlane(player.CameraTarget.forward, Vector3.up);

            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = player.transform.forward;
            }

            forward.Normalize();

            float distance = GetCurrentPlacementDistance();

            Vector3 flatTarget = player.CameraTarget.position + forward * distance;
            flatTarget.y = player.transform.position.y;

            Vector3 rayOrigin = flatTarget + Vector3.up * rayStartHeight;
            Ray ray = new Ray(rayOrigin, Vector3.down);

            if (drawDebugRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.magenta);
            }

            bool foundSurface = Physics.Raycast(
                ray,
                out RaycastHit hit,
                rayDistance,
                placementSurfaceLayerMask,
                surfaceTriggerInteraction
            );

            if (!foundSurface)
            {
                return false;
            }

            if (Vector3.Dot(hit.normal, Vector3.up) < minimumSurfaceUpDot)
            {
                return false;
            }

            position = GetRootPositionWithBottomOnSurface(
                hit.point,
                hit.normal,
                rotation
            );

            return true;
        }

        private float GetCurrentPlacementDistance()
        {
            float baseDistance = Mathf.Min(
                placementDistance,
                currentFurniture.MaxPlacementDistance
            );

            float pitchDelta = player.CameraTarget.forward.y - pickupCameraForwardY;
            float pitchDistanceOffset = pitchDelta * lookPitchDistanceInfluence;

            pitchDistanceOffset = Mathf.Clamp(
                pitchDistanceOffset,
                -maxLookBackwardOffset,
                maxLookForwardOffset
            );

            float minDistance = Mathf.Max(
                0.1f,
                baseDistance - maxLookBackwardOffset
            );

            float maxDistance = Mathf.Min(
                currentFurniture.MaxPlacementDistance,
                baseDistance + maxLookForwardOffset
            );

            return Mathf.Clamp(
                baseDistance + pitchDistanceOffset,
                minDistance,
                maxDistance
            );
        }

        private Vector3 GetRootPositionWithBottomOnSurface(
            Vector3 surfacePoint,
            Vector3 surfaceNormal,
            Quaternion placementRotation)
        {
            BoxCollider bounds = currentFurniture.PlacementBounds;
            Vector3[] localCorners = GetBoundsCornersInFurnitureSpace(bounds);

            float lowestDistanceAlongNormal = float.PositiveInfinity;

            for (int i = 0; i < localCorners.Length; i++)
            {
                Vector3 rotatedCornerOffset = placementRotation * localCorners[i];
                float distanceAlongNormal = Vector3.Dot(rotatedCornerOffset, surfaceNormal);

                if (distanceAlongNormal < lowestDistanceAlongNormal)
                {
                    lowestDistanceAlongNormal = distanceAlongNormal;
                }
            }

            return surfacePoint - surfaceNormal * lowestDistanceAlongNormal;
        }

        private bool IsBlocked(Vector3 position, Quaternion rotation)
        {
            BoxCollider bounds = currentFurniture.PlacementBounds;

            Vector3 center = position + rotation * GetBoundsCenterInFurnitureSpace(bounds);
            Vector3 halfExtents = GetScaledHalfExtents(bounds);

            halfExtents -= Vector3.one * obstructionInset;
            halfExtents = Vector3.Max(halfExtents, Vector3.one * 0.001f);

            return Physics.CheckBox(
                center,
                halfExtents,
                rotation,
                obstructionLayerMask,
                obstructionTriggerInteraction
            );
        }

        private Vector3 GetBoundsCenterInFurnitureSpace(BoxCollider bounds)
        {
            Vector3 worldCenter = bounds.transform.TransformPoint(bounds.center);
            return currentFurniture.transform.InverseTransformPoint(worldCenter);
        }

        private Vector3[] GetBoundsCornersInFurnitureSpace(BoxCollider bounds)
        {
            Vector3 center = bounds.center;
            Vector3 halfSize = bounds.size * 0.5f;

            Vector3[] corners =
            {
                center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
                center + new Vector3(-halfSize.x, -halfSize.y,  halfSize.z),
                center + new Vector3(-halfSize.x,  halfSize.y, -halfSize.z),
                center + new Vector3(-halfSize.x,  halfSize.y,  halfSize.z),

                center + new Vector3( halfSize.x, -halfSize.y, -halfSize.z),
                center + new Vector3( halfSize.x, -halfSize.y,  halfSize.z),
                center + new Vector3( halfSize.x,  halfSize.y, -halfSize.z),
                center + new Vector3( halfSize.x,  halfSize.y,  halfSize.z)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 worldCorner = bounds.transform.TransformPoint(corners[i]);
                corners[i] = currentFurniture.transform.InverseTransformPoint(worldCorner);
            }

            return corners;
        }

        private Vector3 GetScaledHalfExtents(BoxCollider bounds)
        {
            Vector3 scale = bounds.transform.lossyScale;
            Vector3 size = bounds.size;

            return new Vector3(
                Mathf.Abs(size.x * scale.x) * 0.5f,
                Mathf.Abs(size.y * scale.y) * 0.5f,
                Mathf.Abs(size.z * scale.z) * 0.5f
            );
        }

        private Quaternion GetTargetRotation()
        {
            return Quaternion.Euler(
                0f,
                startingYaw + yawOffset,
                0f
            );
        }

        private void ClearState()
        {
            currentFurniture = null;

            originalPosition = Vector3.zero;
            originalRotation = Quaternion.identity;

            targetPosition = Vector3.zero;
            targetRotation = Quaternion.identity;
            positionVelocity = Vector3.zero;

            startingYaw = 0f;
            yawOffset = 0f;
            pickupCameraForwardY = 0f;

            startedFrame = -1;
            canPlace = false;
        }
    }
}