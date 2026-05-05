using UnityEngine;

namespace Project.Placement
{
    public sealed class PlacementObstructionValidator : MonoBehaviour
    {
        [Header("Obstruction Detection")]
        [Tooltip("Layers that can block placement. Usually this should be Everything.")]
        [SerializeField] private LayerMask obstructionLayerMask = ~0;

        [SerializeField] private QueryTriggerInteraction obstructionTriggerInteraction = QueryTriggerInteraction.Ignore;

        [Tooltip("Objects on these layers will not block placement. Usually includes Player and PlacementSurface.")]
        [SerializeField] private LayerMask ignoredObstructionLayers;

        [Tooltip("Slightly shrinks overlap checks so touching nearby surfaces is less likely to count as blocked.")]
        [SerializeField, Range(0f, 0.2f)] private float obstructionCheckInset = 0.02f;

        public bool HasObstruction(
            FurnitureItem furniture,
            Vector3 placementPosition,
            Quaternion placementRotation)
        {
            if (furniture == null)
            {
                return true;
            }

            BoxCollider[] boxColliders = furniture.GetComponentsInChildren<BoxCollider>(true);

            if (boxColliders == null || boxColliders.Length == 0)
            {
                return false;
            }

            foreach (BoxCollider boxCollider in boxColliders)
            {
                if (boxCollider == null)
                {
                    continue;
                }

                Vector3 worldCenter = GetWorldBoxCenterAtPlacement(
                    furniture,
                    boxCollider,
                    placementPosition,
                    placementRotation
                );

                Vector3 halfExtents = GetWorldBoxHalfExtents(boxCollider);

                halfExtents.x = Mathf.Max(0.001f, halfExtents.x - obstructionCheckInset);
                halfExtents.y = Mathf.Max(0.001f, halfExtents.y - obstructionCheckInset);
                halfExtents.z = Mathf.Max(0.001f, halfExtents.z - obstructionCheckInset);

                Quaternion worldRotation = GetWorldBoxRotationAtPlacement(
                    furniture,
                    boxCollider,
                    placementRotation
                );

                Collider[] hits = Physics.OverlapBox(
                    worldCenter,
                    halfExtents,
                    worldRotation,
                    obstructionLayerMask,
                    obstructionTriggerInteraction
                );

                for (int i = 0; i < hits.Length; i++)
                {
                    Collider hit = hits[i];

                    if (ShouldIgnoreHit(furniture, hit))
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        private bool ShouldIgnoreHit(FurnitureItem furniture, Collider hit)
        {
            if (hit == null)
            {
                return true;
            }

            if (furniture == null)
            {
                return true;
            }

            if (hit.transform.IsChildOf(furniture.transform))
            {
                return true;
            }

            int hitLayerMask = 1 << hit.gameObject.layer;

            if ((ignoredObstructionLayers.value & hitLayerMask) != 0)
            {
                return true;
            }

            return false;
        }

        private Vector3 GetWorldBoxCenterAtPlacement(
            FurnitureItem furniture,
            BoxCollider boxCollider,
            Vector3 placementPosition,
            Quaternion placementRotation)
        {
            Vector3 boxWorldCenterNow =
                boxCollider.transform.TransformPoint(boxCollider.center);

            Vector3 localCenterFromFurnitureRoot =
                furniture.transform.InverseTransformPoint(boxWorldCenterNow);

            return placementPosition + placementRotation * localCenterFromFurnitureRoot;
        }

        private Quaternion GetWorldBoxRotationAtPlacement(
            FurnitureItem furniture,
            BoxCollider boxCollider,
            Quaternion placementRotation)
        {
            Quaternion localRotationFromFurnitureRoot =
                Quaternion.Inverse(furniture.transform.rotation) *
                boxCollider.transform.rotation;

            return placementRotation * localRotationFromFurnitureRoot;
        }

        private Vector3 GetWorldBoxHalfExtents(BoxCollider boxCollider)
        {
            Vector3 localScale = boxCollider.transform.lossyScale;
            Vector3 size = boxCollider.size;

            return new Vector3(
                Mathf.Abs(size.x * localScale.x) * 0.5f,
                Mathf.Abs(size.y * localScale.y) * 0.5f,
                Mathf.Abs(size.z * localScale.z) * 0.5f
            );
        }
    }
}