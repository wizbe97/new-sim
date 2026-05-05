using UnityEngine;

namespace Project.Placement
{
    public sealed class FurnitureItem : MonoBehaviour
    {
        [Header("Placement")]
        [SerializeField, Min(0.1f)] private float maxPlacementDistance = 4f;

        [Tooltip("How far above the hit surface this furniture pivot should be placed. For a 1-unit cube with a centered pivot, use 0.5.")]
        [SerializeField, Min(0f)] private float surfaceOffset = 0.5f;

        [Header("Preview Visuals")]
        [Tooltip("If false, all renderers on this object and its children are used automatically.")]
        [SerializeField] private bool useExplicitPreviewRenderers;

        [Tooltip("Only used if Use Explicit Preview Renderers is enabled.")]
        [SerializeField] private Renderer[] explicitPreviewRenderers;

        public float MaxPlacementDistance => maxPlacementDistance;
        public float SurfaceOffset => surfaceOffset;

        public Renderer[] GetPreviewRenderers()
        {
            if (useExplicitPreviewRenderers)
            {
                return explicitPreviewRenderers;
            }

            return GetComponentsInChildren<Renderer>(true);
        }
    }
}