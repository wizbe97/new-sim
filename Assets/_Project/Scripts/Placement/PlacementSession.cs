using Project.Player;
using UnityEngine;

namespace Project.Placement
{
    public sealed class PlacementSession
    {
        public FurnitureItem Furniture { get; }

        public Vector3 OriginalPosition { get; }
        public Quaternion OriginalRotation { get; }

        public Vector3 CurrentPlacementPosition { get; private set; }

        public bool HasPlacementControlActivated { get; private set; }
        public bool HasValidSurface { get; private set; }
        public bool HasObstruction { get; private set; }

        public int StartedFrame { get; }

        private Vector3 pickupLocalFlatOffset;
        private float pickupDistance;
        private float pickupCameraForwardY;
        private Quaternion pickupLocalRotationOffset;

        private float manualYawRotationDegrees;

        public bool CanPlace => HasValidSurface && !HasObstruction;

        private PlacementSession(
            FurnitureItem furniture,
            Vector3 originalPosition,
            Quaternion originalRotation,
            Vector3 pickupLocalFlatOffset,
            float pickupDistance,
            float pickupCameraForwardY,
            Quaternion pickupLocalRotationOffset)
        {
            Furniture = furniture;

            OriginalPosition = originalPosition;
            OriginalRotation = originalRotation;

            CurrentPlacementPosition = originalPosition;

            this.pickupLocalFlatOffset = pickupLocalFlatOffset;
            this.pickupDistance = pickupDistance;
            this.pickupCameraForwardY = pickupCameraForwardY;
            this.pickupLocalRotationOffset = pickupLocalRotationOffset;

            manualYawRotationDegrees = 0f;

            HasValidSurface = true;
            HasObstruction = false;
            HasPlacementControlActivated = false;

            StartedFrame = Time.frameCount;
        }

        public static PlacementSession Create(
            FurnitureItem furniture,
            FirstPersonController player,
            float minimumPlacementDistance)
        {
            Vector3 originalPosition = furniture.transform.position;
            Quaternion originalRotation = furniture.transform.rotation;

            Vector3 pickupLocalFlatOffset = Vector3.forward;
            float pickupDistance = minimumPlacementDistance;
            float pickupCameraForwardY = 0f;
            Quaternion pickupLocalRotationOffset = Quaternion.identity;

            if (player != null && player.CameraTarget != null)
            {
                Vector3 cameraFlatPosition = Vector3.ProjectOnPlane(player.CameraTarget.position, Vector3.up);
                Vector3 furnitureFlatPosition = Vector3.ProjectOnPlane(furniture.transform.position, Vector3.up);

                Vector3 worldFlatOffset = furnitureFlatPosition - cameraFlatPosition;

                if (worldFlatOffset.sqrMagnitude <= 0.001f)
                {
                    worldFlatOffset = player.transform.forward * minimumPlacementDistance;
                }

                pickupLocalFlatOffset = Quaternion.Inverse(player.transform.rotation) * worldFlatOffset;
                pickupLocalFlatOffset.y = 0f;

                pickupDistance = Mathf.Clamp(
                    pickupLocalFlatOffset.magnitude,
                    minimumPlacementDistance,
                    furniture.MaxPlacementDistance
                );

                pickupLocalFlatOffset = pickupLocalFlatOffset.normalized * pickupDistance;

                pickupCameraForwardY = player.CameraTarget.forward.y;

                pickupLocalRotationOffset =
                    Quaternion.Inverse(player.transform.rotation) *
                    furniture.transform.rotation;
            }

            return new PlacementSession(
                furniture,
                originalPosition,
                originalRotation,
                pickupLocalFlatOffset,
                pickupDistance,
                pickupCameraForwardY,
                pickupLocalRotationOffset
            );
        }

        public void ActivatePlacementControl()
        {
            HasPlacementControlActivated = true;
        }

        public void SetPlacementPosition(Vector3 position)
        {
            CurrentPlacementPosition = position;
        }

        public void SetValidationState(bool hasValidSurface, bool hasObstruction)
        {
            HasValidSurface = hasValidSurface;
            HasObstruction = hasObstruction;
        }

        public void RotateYaw(float degrees)
        {
            manualYawRotationDegrees += degrees;

            if (manualYawRotationDegrees >= 360f || manualYawRotationDegrees <= -360f)
            {
                manualYawRotationDegrees %= 360f;
            }
        }

        public Vector3 GetDesiredFlatPlacementPosition(
            FirstPersonController player,
            float minimumPlacementDistance,
            float pitchDistanceInfluence)
        {
            Vector3 cameraFlatPosition = Vector3.ProjectOnPlane(player.CameraTarget.position, Vector3.up);

            Vector3 localDirection = pickupLocalFlatOffset.sqrMagnitude > 0.001f
                ? pickupLocalFlatOffset.normalized
                : Vector3.forward;

            float pitchDelta = player.CameraTarget.forward.y - pickupCameraForwardY;

            float desiredDistance = pickupDistance + pitchDelta * pitchDistanceInfluence;

            desiredDistance = Mathf.Clamp(
                desiredDistance,
                minimumPlacementDistance,
                Furniture.MaxPlacementDistance
            );

            Vector3 adjustedLocalOffset = localDirection * desiredDistance;
            Vector3 adjustedWorldOffset = player.transform.rotation * adjustedLocalOffset;

            adjustedWorldOffset.y = 0f;

            return cameraFlatPosition + adjustedWorldOffset;
        }

        public Quaternion GetCurrentPreviewRotation(FirstPersonController player)
        {
            if (player == null)
            {
                return OriginalRotation;
            }

            Quaternion baseRotation =
                player.transform.rotation *
                pickupLocalRotationOffset;

            Quaternion manualYawRotation =
                Quaternion.Euler(0f, manualYawRotationDegrees, 0f);

            return baseRotation * manualYawRotation;
        }

        public void ApplyPreviewTransform(FirstPersonController player, float moveSpeed)
        {
            if (Furniture == null)
            {
                return;
            }

            if (!HasValidSurface)
            {
                return;
            }

            Furniture.transform.position = Vector3.Lerp(
                Furniture.transform.position,
                CurrentPlacementPosition,
                moveSpeed * Time.deltaTime
            );

            Furniture.transform.rotation = GetCurrentPreviewRotation(player);
        }

        public void ApplyCurrentRotation(FirstPersonController player)
        {
            if (Furniture == null)
            {
                return;
            }

            Furniture.transform.rotation = GetCurrentPreviewRotation(player);
        }

        public void Commit(FirstPersonController player)
        {
            if (Furniture == null)
            {
                return;
            }

            Furniture.transform.SetPositionAndRotation(
                CurrentPlacementPosition,
                GetCurrentPreviewRotation(player)
            );
        }

        public void Cancel()
        {
            if (Furniture == null)
            {
                return;
            }

            Furniture.transform.SetPositionAndRotation(
                OriginalPosition,
                OriginalRotation
            );
        }
    }
}