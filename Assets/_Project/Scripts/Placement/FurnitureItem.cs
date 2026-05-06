using Project.Shop;
using UnityEngine;

namespace Project.Placement
{
    public sealed class FurnitureItem : MonoBehaviour
    {
        [Header("Shop Data")]
        [SerializeField] private StoreItemSO storeItem;

        [Header("Placement")]
        [SerializeField, Min(0.1f)] private float maxPlacementDistance = 4f;

        [Tooltip("No longer used by the current BuildingManager, but kept for existing prefab compatibility.")]
        [SerializeField, Min(0f)] private float surfaceOffset = 0.5f;

        [Tooltip("The BoxCollider used for placement obstruction checks. If empty, the first child BoxCollider is used.")]
        [SerializeField] private BoxCollider placementBounds;

        [Header("Preview Visuals")]
        [SerializeField] private bool useExplicitPreviewRenderers;
        [SerializeField] private Renderer[] explicitPreviewRenderers;

        private Renderer[] cachedRenderers;
        private Material[][] originalMaterials;
        private Collider[] cachedColliders;
        private bool[] originalColliderStates;
        private bool previewActive;

        public StoreItemSO StoreItem => storeItem;
        public float MaxPlacementDistance => maxPlacementDistance;
        public float SurfaceOffset => surfaceOffset;

        public BoxCollider PlacementBounds
        {
            get
            {
                if (placementBounds == null)
                {
                    placementBounds = GetComponentInChildren<BoxCollider>(true);
                }

                return placementBounds;
            }
        }

        public void SetStoreItem(StoreItemSO item)
        {
            storeItem = item;
        }

        public void BeginPreview()
        {
            if (previewActive)
            {
                return;
            }

            CacheMaterials();
            CacheColliders();
            SetCollidersEnabled(false);

            previewActive = true;
        }

        public void EndPreview()
        {
            RestoreMaterials();
            RestoreColliders();

            cachedRenderers = null;
            originalMaterials = null;
            cachedColliders = null;
            originalColliderStates = null;
            previewActive = false;
        }

        public void SetPreviewMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (cachedRenderers == null || cachedRenderers.Length == 0)
            {
                CacheMaterials();
            }

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer previewRenderer = cachedRenderers[i];

                if (previewRenderer == null)
                {
                    continue;
                }

                Material[] materials = previewRenderer.sharedMaterials;

                for (int j = 0; j < materials.Length; j++)
                {
                    materials[j] = material;
                }

                previewRenderer.sharedMaterials = materials;
            }
        }

        private void CacheMaterials()
        {
            cachedRenderers = GetPreviewRenderers();
            originalMaterials = new Material[cachedRenderers.Length][];

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer previewRenderer = cachedRenderers[i];

                if (previewRenderer != null)
                {
                    originalMaterials[i] = previewRenderer.sharedMaterials;
                }
            }
        }

        private void RestoreMaterials()
        {
            if (cachedRenderers == null || originalMaterials == null)
            {
                return;
            }

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer previewRenderer = cachedRenderers[i];

                if (previewRenderer != null && i < originalMaterials.Length && originalMaterials[i] != null)
                {
                    previewRenderer.sharedMaterials = originalMaterials[i];
                }
            }
        }

        public Renderer[] GetPreviewRenderers()
        {
            return useExplicitPreviewRenderers
                ? explicitPreviewRenderers
                : GetComponentsInChildren<Renderer>(true);
        }

        private void CacheColliders()
        {
            cachedColliders = GetComponentsInChildren<Collider>(true);
            originalColliderStates = new bool[cachedColliders.Length];

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                originalColliderStates[i] = cachedColliders[i] != null && cachedColliders[i].enabled;
            }
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (cachedColliders == null)
            {
                return;
            }

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    cachedColliders[i].enabled = enabled;
                }
            }
        }

        private void RestoreColliders()
        {
            if (cachedColliders == null || originalColliderStates == null)
            {
                return;
            }

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null && i < originalColliderStates.Length)
                {
                    cachedColliders[i].enabled = originalColliderStates[i];
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (placementBounds == null)
            {
                placementBounds = GetComponentInChildren<BoxCollider>(true);
            }

            Rigidbody rigidbody = GetComponentInChildren<Rigidbody>(true);

            if (rigidbody != null)
            {
                Debug.LogError(
                    $"{nameof(FurnitureItem)} '{name}' has a Rigidbody on '{rigidbody.name}'. " +
                    "Remove it from the furniture prefab. Only delivery boxes should have Rigidbody components.",
                    rigidbody
                );
            }
        }
#endif
    }
}