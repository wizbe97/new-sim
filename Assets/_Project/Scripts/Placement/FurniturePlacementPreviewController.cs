using UnityEngine;

namespace Project.Placement
{
    [DisallowMultipleComponent]
    public sealed class FurniturePlacementPreviewController : MonoBehaviour
    {
        [Header("Preview Smoothing")]
        [SerializeField, Min(0f)] private float positionSmoothTime = 0.04f;
        [SerializeField, Min(0f)] private float rotationSmoothSpeed = 18f;

        [Header("Preview Materials")]
        [SerializeField] private Material validPreviewMaterial;
        [SerializeField] private Material invalidPreviewMaterial;

        private Vector3 positionVelocity;

        public void BeginPreview(FurnitureItem furniture)
        {
            if (furniture == null)
            {
                return;
            }

            positionVelocity = Vector3.zero;
            furniture.BeginPreview();
        }

        public void UpdatePreview(
            FurnitureItem furniture,
            Vector3 targetPosition,
            Quaternion targetRotation,
            bool canPlace,
            bool immediate)
        {
            if (furniture == null)
            {
                return;
            }

            if (immediate || positionSmoothTime <= 0f)
            {
                furniture.transform.SetPositionAndRotation(targetPosition, targetRotation);
            }
            else
            {
                furniture.transform.position = Vector3.SmoothDamp(
                    furniture.transform.position,
                    targetPosition,
                    ref positionVelocity,
                    positionSmoothTime);

                furniture.transform.rotation = Quaternion.Slerp(
                    furniture.transform.rotation,
                    targetRotation,
                    rotationSmoothSpeed * Time.deltaTime);
            }

            furniture.SetPreviewMaterial(
                canPlace ? validPreviewMaterial : invalidPreviewMaterial);
        }

        public void ApplyInvalidPreview(FurnitureItem furniture)
        {
            if (furniture == null)
            {
                return;
            }

            furniture.SetPreviewMaterial(invalidPreviewMaterial);
        }

        public void EndPreview(FurnitureItem furniture)
        {
            if (furniture == null)
            {
                return;
            }

            furniture.EndPreview();
            Clear();
        }

        public void Clear()
        {
            positionVelocity = Vector3.zero;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            positionSmoothTime = Mathf.Max(0f, positionSmoothTime);
            rotationSmoothSpeed = Mathf.Max(0f, rotationSmoothSpeed);
        }
#endif
    }
}