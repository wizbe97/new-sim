using UnityEngine;

namespace Project.Placement
{
    public sealed class PlacementPreviewVisual : MonoBehaviour
    {
        [Header("Preview Materials")]
        [SerializeField] private Material validPreviewMaterial;
        [SerializeField] private Material invalidPreviewMaterial;

        private FurnitureItem currentFurniture;
        private Renderer[] currentPreviewRenderers;
        private Material[][] originalPreviewMaterials;
        private bool? currentValidState;

        public void BeginPreview(FurnitureItem furniture)
        {
            EndPreview();

            currentFurniture = furniture;

            if (currentFurniture == null)
            {
                return;
            }

            CacheOriginalMaterials();

            currentValidState = null;
        }

        public void SetValidState(bool isValid)
        {
            if (currentFurniture == null)
            {
                return;
            }

            if (currentValidState.HasValue && currentValidState.Value == isValid)
            {
                return;
            }

            currentValidState = isValid;

            Material targetMaterial = isValid
                ? validPreviewMaterial
                : invalidPreviewMaterial;

            ApplyPreviewMaterial(targetMaterial);
        }

        public void EndPreview()
        {
            RestoreOriginalMaterials();

            currentFurniture = null;
            currentPreviewRenderers = null;
            originalPreviewMaterials = null;
            currentValidState = null;
        }

        private void CacheOriginalMaterials()
        {
            currentPreviewRenderers = currentFurniture.GetPreviewRenderers();

            if (currentPreviewRenderers == null || currentPreviewRenderers.Length == 0)
            {
                return;
            }

            originalPreviewMaterials = new Material[currentPreviewRenderers.Length][];

            for (int i = 0; i < currentPreviewRenderers.Length; i++)
            {
                Renderer renderer = currentPreviewRenderers[i];

                if (renderer == null)
                {
                    continue;
                }

                originalPreviewMaterials[i] = renderer.sharedMaterials;
            }
        }

        private void RestoreOriginalMaterials()
        {
            if (currentPreviewRenderers == null || originalPreviewMaterials == null)
            {
                return;
            }

            for (int i = 0; i < currentPreviewRenderers.Length; i++)
            {
                Renderer renderer = currentPreviewRenderers[i];

                if (renderer == null)
                {
                    continue;
                }

                if (i >= originalPreviewMaterials.Length)
                {
                    continue;
                }

                if (originalPreviewMaterials[i] == null)
                {
                    continue;
                }

                renderer.sharedMaterials = originalPreviewMaterials[i];
            }
        }

        private void ApplyPreviewMaterial(Material previewMaterial)
        {
            if (previewMaterial == null)
            {
                return;
            }

            if (currentPreviewRenderers == null || currentPreviewRenderers.Length == 0)
            {
                return;
            }

            for (int i = 0; i < currentPreviewRenderers.Length; i++)
            {
                Renderer renderer = currentPreviewRenderers[i];

                if (renderer == null)
                {
                    continue;
                }

                Material[] currentMaterials = renderer.sharedMaterials;

                if (currentMaterials == null || currentMaterials.Length == 0)
                {
                    continue;
                }

                Material[] previewMaterials = new Material[currentMaterials.Length];

                for (int j = 0; j < previewMaterials.Length; j++)
                {
                    previewMaterials[j] = previewMaterial;
                }

                renderer.sharedMaterials = previewMaterials;
            }
        }
    }
}