using UnityEngine;

namespace Project.Placement
{
    public sealed class PlacementSurfaceDetector : MonoBehaviour
    {
        [Header("Surface Detection")]
        [SerializeField] private LayerMask placementSurfaceLayerMask = ~0;
        [SerializeField] private QueryTriggerInteraction surfaceTriggerInteraction = QueryTriggerInteraction.Ignore;

        [Tooltip("Minimum upward direction required for a surface to count as placeable. 0.7 means roughly 45 degrees or flatter.")]
        [SerializeField, Range(0f, 1f)] private float minimumSurfaceUpDot = 0.7f;

        [Header("Ground Search")]
        [Tooltip("How far above the desired placement point the downward ray starts.")]
        [SerializeField, Min(0.1f)] private float downcastStartHeight = 5f;

        [Tooltip("How far downward the placement ray checks for a placement surface.")]
        [SerializeField, Min(0.1f)] private float downcastDistance = 10f;

        [Header("Debug")]
        [SerializeField] private bool drawDebugRay = true;

        public bool TryFindPlacementSurface(
            FurnitureItem furniture,
            Vector3 desiredFlatPosition,
            out Vector3 placementPosition)
        {
            placementPosition = Vector3.zero;

            if (furniture == null)
            {
                return false;
            }

            Vector3 rayOrigin =
                desiredFlatPosition +
                Vector3.up * downcastStartHeight;

            Ray downRay = new Ray(rayOrigin, Vector3.down);

            if (drawDebugRay)
            {
                Debug.DrawRay(
                    downRay.origin,
                    downRay.direction * downcastDistance,
                    Color.magenta
                );
            }

            bool hitSurface = Physics.Raycast(
                downRay,
                out RaycastHit hit,
                downcastDistance,
                placementSurfaceLayerMask,
                surfaceTriggerInteraction
            );

            if (!hitSurface)
            {
                return false;
            }

            if (!IsValidSurfaceNormal(hit.normal))
            {
                return false;
            }

            placementPosition =
                hit.point +
                hit.normal * furniture.SurfaceOffset;

            return true;
        }

        private bool IsValidSurfaceNormal(Vector3 surfaceNormal)
        {
            return Vector3.Dot(surfaceNormal, Vector3.up) >= minimumSurfaceUpDot;
        }
    }
}