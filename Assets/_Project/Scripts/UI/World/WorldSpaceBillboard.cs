using UnityEngine;

namespace Project.UI.World
{
    public sealed class WorldSpaceBillboard : MonoBehaviour
    {
        [Header("Optional Target")]
        [Tooltip("Optional. Leave empty on prefabs. The script will automatically find the active Unity Camera at runtime.")]
        [SerializeField] private Camera targetCamera;

        [Header("Behaviour")]
        [Tooltip("Keeps the UI upright and only rotates around the Y axis.")]
        [SerializeField] private bool lockToYAxis = true;

        [Tooltip("Enable this if your text faces away from the camera.")]
        [SerializeField] private bool invertForward = true;

        [Tooltip("How often the script retries finding a camera if none is assigned.")]
        [SerializeField, Min(0.1f)] private float cameraSearchIntervalSeconds = 0.5f;

        private Transform targetCameraTransform;
        private float nextCameraSearchTime;

        private void OnEnable()
        {
            ResolveCamera(force: true);
        }

        private void LateUpdate()
        {
            if (targetCameraTransform == null)
            {
                ResolveCamera(force: false);

                if (targetCameraTransform == null)
                {
                    return;
                }
            }

            FaceCamera();
        }

        private void ResolveCamera(bool force)
        {
            if (!force && Time.time < nextCameraSearchTime)
            {
                return;
            }

            nextCameraSearchTime = Time.time + cameraSearchIntervalSeconds;

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                targetCamera = FindFirstObjectByType<Camera>();
            }

            targetCameraTransform = targetCamera != null
                ? targetCamera.transform
                : null;
        }

        private void FaceCamera()
        {
            Vector3 directionToCamera = targetCameraTransform.position - transform.position;

            if (lockToYAxis)
            {
                directionToCamera.y = 0f;
            }

            if (directionToCamera.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 lookDirection = invertForward
                ? -directionToCamera.normalized
                : directionToCamera.normalized;

            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
    }
}