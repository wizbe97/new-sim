using UnityEngine;

namespace Project.Placement
{
    [DisallowMultipleComponent]
    public sealed class FurniturePlacementValidator : MonoBehaviour
    {
        [Header("Obstruction Detection")]
        [Tooltip("Exclude Player and PlacementSurface from this mask.")]
        [SerializeField] private LayerMask obstructionLayerMask = ~0;

        [SerializeField] private QueryTriggerInteraction obstructionTriggerInteraction = QueryTriggerInteraction.Ignore;

        [SerializeField, Range(0f, 0.2f)] private float obstructionInset = 0.02f;

        public bool CanPlace(
            FurnitureItem furniture,
            Vector3 position,
            Quaternion rotation)
        {
            if (furniture == null)
            {
                return false;
            }

            if (furniture.PlacementBounds == null)
            {
                return false;
            }

            return !IsBlocked(furniture, position, rotation);
        }

        private bool IsBlocked(
            FurnitureItem furniture,
            Vector3 position,
            Quaternion rotation)
        {
            BoxCollider bounds = furniture.PlacementBounds;

            Vector3 centerOffset = GetBoundsCenterOffsetFromFurnitureRoot(
                furniture,
                bounds);

            Vector3 center = position + rotation * centerOffset;
            Vector3 halfExtents = GetScaledHalfExtents(bounds);

            halfExtents -= Vector3.one * obstructionInset;
            halfExtents = Vector3.Max(halfExtents, Vector3.one * 0.001f);

            return Physics.CheckBox(
                center,
                halfExtents,
                rotation,
                obstructionLayerMask,
                obstructionTriggerInteraction);
        }

        private static Vector3 GetBoundsCenterOffsetFromFurnitureRoot(
            FurnitureItem furniture,
            BoxCollider bounds)
        {
            Vector3 worldCenter = bounds.transform.TransformPoint(bounds.center);
            Vector3 worldOffsetFromRoot = worldCenter - furniture.transform.position;

            return Quaternion.Inverse(furniture.transform.rotation) * worldOffsetFromRoot;
        }

        private static Vector3 GetScaledHalfExtents(BoxCollider bounds)
        {
            Vector3 scale = bounds.transform.lossyScale;
            Vector3 size = bounds.size;

            return new Vector3(
                Mathf.Abs(size.x * scale.x) * 0.5f,
                Mathf.Abs(size.y * scale.y) * 0.5f,
                Mathf.Abs(size.z * scale.z) * 0.5f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            obstructionInset = Mathf.Clamp(obstructionInset, 0f, 0.2f);
        }
#endif
    }
}