using Project.Player;
using UnityEngine;

namespace Project.Placement
{
    [DisallowMultipleComponent]
    public sealed class FurniturePlacementPoseProvider : MonoBehaviour
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

        [Header("Surface Detection")]
        [SerializeField] private LayerMask placementSurfaceLayerMask = ~0;
        [SerializeField] private QueryTriggerInteraction surfaceTriggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField, Range(0f, 1f)] private float minimumSurfaceUpDot = 0.7f;
        [SerializeField, Min(0.1f)] private float rayStartHeight = 3f;
        [SerializeField, Min(0.1f)] private float rayDistance = 8f;

        [Header("Debug")]
        [SerializeField] private bool drawDebugRay = true;

        private FirstPersonController player;
        private float startingYaw;
        private float pickupCameraForwardY;

        public void Initialize(FirstPersonController newPlayer)
        {
            if (newPlayer == null)
            {
                Debug.LogError($"{nameof(FurniturePlacementPoseProvider)} cannot initialize because player is missing.", this);
                return;
            }

            player = newPlayer;
        }

        public void BeginPlacement(
            FurnitureItem furniture,
            float newStartingYaw,
            float newPickupCameraForwardY)
        {
            if (furniture == null)
            {
                return;
            }

            startingYaw = newStartingYaw;
            pickupCameraForwardY = newPickupCameraForwardY;
        }

        public bool TryGetPlacementPose(
            FurnitureItem furniture,
            float yawOffset,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = GetTargetRotation(yawOffset);

            if (furniture == null)
            {
                return false;
            }

            if (player == null || player.CameraTarget == null)
            {
                return false;
            }

            if (furniture.PlacementBounds == null)
            {
                return false;
            }

            Vector3 forward = Vector3.ProjectOnPlane(player.CameraTarget.forward, Vector3.up);

            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = player.transform.forward;
            }

            forward.Normalize();

            float distance = GetCurrentPlacementDistance(furniture);

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
                surfaceTriggerInteraction);

            if (!foundSurface)
            {
                return false;
            }

            if (Vector3.Dot(hit.normal, Vector3.up) < minimumSurfaceUpDot)
            {
                return false;
            }

            position = GetRootPositionWithBottomOnSurface(
                furniture,
                hit.point,
                hit.normal,
                rotation);

            return true;
        }

        public void Clear()
        {
            startingYaw = 0f;
            pickupCameraForwardY = 0f;
        }

        private float GetCurrentPlacementDistance(FurnitureItem furniture)
        {
            float baseDistance = Mathf.Min(
                placementDistance,
                furniture.MaxPlacementDistance);

            float pitchDelta = player.CameraTarget.forward.y - pickupCameraForwardY;
            float pitchDistanceOffset = pitchDelta * lookPitchDistanceInfluence;

            pitchDistanceOffset = Mathf.Clamp(
                pitchDistanceOffset,
                -maxLookBackwardOffset,
                maxLookForwardOffset);

            float minDistance = Mathf.Max(
                0.1f,
                baseDistance - maxLookBackwardOffset);

            float maxDistance = Mathf.Min(
                furniture.MaxPlacementDistance,
                baseDistance + maxLookForwardOffset);

            return Mathf.Clamp(
                baseDistance + pitchDistanceOffset,
                minDistance,
                maxDistance);
        }

        private Vector3 GetRootPositionWithBottomOnSurface(
            FurnitureItem furniture,
            Vector3 surfacePoint,
            Vector3 surfaceNormal,
            Quaternion placementRotation)
        {
            BoxCollider bounds = furniture.PlacementBounds;
            Vector3[] rootSpaceCornerOffsets = GetBoundsCornerOffsetsFromFurnitureRoot(
                furniture,
                bounds);

            float lowestDistanceAlongNormal = float.PositiveInfinity;

            for (int i = 0; i < rootSpaceCornerOffsets.Length; i++)
            {
                Vector3 targetWorldOffset = placementRotation * rootSpaceCornerOffsets[i];
                float distanceAlongNormal = Vector3.Dot(targetWorldOffset, surfaceNormal);

                if (distanceAlongNormal < lowestDistanceAlongNormal)
                {
                    lowestDistanceAlongNormal = distanceAlongNormal;
                }
            }

            return surfacePoint - surfaceNormal * lowestDistanceAlongNormal;
        }

        private static Vector3[] GetBoundsCornerOffsetsFromFurnitureRoot(
            FurnitureItem furniture,
            BoxCollider bounds)
        {
            Vector3 center = bounds.center;
            Vector3 halfSize = bounds.size * 0.5f;

            Vector3[] localCorners =
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

            Quaternion inverseFurnitureRotation =
                Quaternion.Inverse(furniture.transform.rotation);

            Vector3 furnitureRootPosition = furniture.transform.position;

            for (int i = 0; i < localCorners.Length; i++)
            {
                Vector3 worldCorner = bounds.transform.TransformPoint(localCorners[i]);
                Vector3 worldOffsetFromRoot = worldCorner - furnitureRootPosition;

                localCorners[i] = inverseFurnitureRotation * worldOffsetFromRoot;
            }

            return localCorners;
        }

        private Quaternion GetTargetRotation(float yawOffset)
        {
            return Quaternion.Euler(
                0f,
                startingYaw + yawOffset,
                0f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            placementDistance = Mathf.Max(0.1f, placementDistance);
            lookPitchDistanceInfluence = Mathf.Max(0f, lookPitchDistanceInfluence);
            maxLookForwardOffset = Mathf.Max(0f, maxLookForwardOffset);
            maxLookBackwardOffset = Mathf.Max(0f, maxLookBackwardOffset);

            minimumSurfaceUpDot = Mathf.Clamp01(minimumSurfaceUpDot);
            rayStartHeight = Mathf.Max(0.1f, rayStartHeight);
            rayDistance = Mathf.Max(0.1f, rayDistance);
        }
#endif
    }
}