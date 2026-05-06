using Project.Input;
using Project.Placement;
using Project.Player;
using UnityEngine;

namespace Project.Managers
{
    [RequireComponent(typeof(PlacementSurfaceDetector))]
    [RequireComponent(typeof(PlacementObstructionValidator))]
    [RequireComponent(typeof(PlacementPreviewVisual))]
    public sealed class PlacementManager : MonoBehaviour
    {
        [Header("Placement Preview")]
        [SerializeField, Min(0f)] private float previewMoveSpeed = 18f;

        [Tooltip("Minimum horizontal distance the furniture can be from the player while placing.")]
        [SerializeField, Min(0.1f)] private float minPlacementDistance = 0.75f;

        [Tooltip("How much mouse-look input is required before the held item starts actively following the player's aim.")]
        [SerializeField, Min(0f)] private float lookActivationThreshold = 0.01f;

        [Tooltip("How much looking up/down changes the furniture distance. Looking up moves it further, looking down moves it closer.")]
        [SerializeField, Min(0f)] private float pitchDistanceInfluence = 2f;

        [Header("Rotation")]
        [SerializeField] private float rotationStepDegrees = 15f;

        private FirstPersonController player;
        private PlayerInputHandler input;

        private PlacementSurfaceDetector surfaceDetector;
        private PlacementObstructionValidator obstructionValidator;
        private PlacementPreviewVisual previewVisual;

        private PlacementSession currentSession;

        public bool IsPlacing => currentSession != null;
        public bool CanPlace => IsPlacing && currentSession.CanPlace;

        public FurnitureItem CurrentFurniture => currentSession != null
            ? currentSession.Furniture
            : null;

        private void Awake()
        {
            surfaceDetector = GetComponent<PlacementSurfaceDetector>();
            obstructionValidator = GetComponent<PlacementObstructionValidator>();
            previewVisual = GetComponent<PlacementPreviewVisual>();
        }

        public void Initialize(FirstPersonController newPlayer)
        {
            player = newPlayer;

            if (player == null)
            {
                Debug.LogError($"{nameof(PlacementManager)} cannot initialize without a player.");
                return;
            }

            input = player.GetComponent<PlayerInputHandler>();

            if (input == null)
            {
                Debug.LogError($"{nameof(PlacementManager)} could not find {nameof(PlayerInputHandler)} on player.");
                return;
            }

            input.InteractPressed += HandleInteractPressed;
            input.CancelPressed += HandleCancelPressed;
            input.RotatePressed += HandleRotatePressed;
        }

        private void OnDestroy()
        {
            if (input == null)
            {
                return;
            }

            input.InteractPressed -= HandleInteractPressed;
            input.CancelPressed -= HandleCancelPressed;
            input.RotatePressed -= HandleRotatePressed;
        }

        private void Update()
        {
            if (!IsPlacing)
            {
                return;
            }

            UpdatePlacement();
        }

        public void BeginPlacement(FurnitureItem furniture)
        {
            if (furniture == null)
            {
                Debug.LogWarning($"{nameof(PlacementManager)} tried to place a null furniture item.");
                return;
            }

            if (IsPlacing)
            {
                return;
            }

            currentSession = PlacementSession.Create(
                furniture,
                player,
                minPlacementDistance
            );

            previewVisual.BeginPreview(furniture);

            EvaluateCurrentPlacementWithoutMoving();

            Debug.Log($"Started placing {furniture.name}");
        }

        public FurnitureItem TakeCurrentFurnitureForBoxing()
        {
            if (!IsPlacing)
            {
                return null;
            }

            FurnitureItem furniture = currentSession.Furniture;

            previewVisual.EndPreview();

            Debug.Log($"Boxing up {furniture.name}");

            currentSession = null;

            return furniture;
        }

        private void UpdatePlacement()
        {
            if (!currentSession.HasPlacementControlActivated)
            {
                bool hasMeaningfulLookInput =
                    input != null &&
                    input.LookInput.sqrMagnitude > lookActivationThreshold * lookActivationThreshold;

                if (!hasMeaningfulLookInput)
                {
                    EvaluateCurrentPlacementWithoutMoving();
                    return;
                }

                currentSession.ActivatePlacementControl();
            }

            EvaluateCurrentPlacementFromAim();

            currentSession.ApplyPreviewTransform(
                player,
                previewMoveSpeed
            );
        }

        private void EvaluateCurrentPlacementWithoutMoving()
        {
            if (!IsPlacing)
            {
                return;
            }

            FurnitureItem furniture = currentSession.Furniture;

            Vector3 position = furniture.transform.position;
            Quaternion rotation = currentSession.GetCurrentPreviewRotation(player);

            furniture.transform.rotation = rotation;
            currentSession.SetPlacementPosition(position);

            bool hasObstruction = obstructionValidator.HasObstruction(
                furniture,
                position,
                rotation
            );

            currentSession.SetValidationState(
                hasValidSurface: true,
                hasObstruction: hasObstruction
            );

            previewVisual.SetValidState(currentSession.CanPlace);
        }

        private void EvaluateCurrentPlacementFromAim()
        {
            if (!IsPlacing)
            {
                return;
            }

            Vector3 desiredFlatPosition =
                currentSession.GetDesiredFlatPlacementPosition(
                    player,
                    minPlacementDistance,
                    pitchDistanceInfluence
                );

            bool hasSurface = surfaceDetector.TryFindPlacementSurface(
                currentSession.Furniture,
                desiredFlatPosition,
                out Vector3 placementPosition
            );

            bool hasObstruction = false;

            if (hasSurface)
            {
                Quaternion placementRotation = currentSession.GetCurrentPreviewRotation(player);

                hasObstruction = obstructionValidator.HasObstruction(
                    currentSession.Furniture,
                    placementPosition,
                    placementRotation
                );

                currentSession.SetPlacementPosition(placementPosition);
            }

            currentSession.SetValidationState(
                hasValidSurface: hasSurface,
                hasObstruction: hasObstruction
            );

            previewVisual.SetValidState(currentSession.CanPlace);
        }

        private void PlaceCurrentFurniture()
        {
            if (!IsPlacing)
            {
                return;
            }

            if (!currentSession.CanPlace)
            {
                Debug.Log("Cannot place furniture: invalid surface or blocked by another object.");
                return;
            }

            FurnitureItem furniture = currentSession.Furniture;

            currentSession.Commit(player);
            previewVisual.EndPreview();

            Debug.Log($"Placed {furniture.name}");

            currentSession = null;
        }

        private void CancelPlacement()
        {
            if (!IsPlacing)
            {
                return;
            }

            FurnitureItem furniture = currentSession.Furniture;

            currentSession.Cancel();
            previewVisual.EndPreview();

            Debug.Log($"Cancelled placing {furniture.name}");

            currentSession = null;
        }

        private void RotateCurrentFurniture()
        {
            if (!IsPlacing)
            {
                return;
            }

            currentSession.RotateYaw(rotationStepDegrees);
            currentSession.ApplyCurrentRotation(player);

            if (currentSession.HasPlacementControlActivated)
            {
                EvaluateCurrentPlacementFromAim();
            }
            else
            {
                EvaluateCurrentPlacementWithoutMoving();
            }
        }

        private void HandleInteractPressed()
        {
            if (!IsPlacing)
            {
                return;
            }

            if (Time.frameCount == currentSession.StartedFrame)
            {
                return;
            }

            PlaceCurrentFurniture();
        }

        private void HandleCancelPressed()
        {
            if (!IsPlacing)
            {
                return;
            }

            CancelPlacement();
        }

        private void HandleRotatePressed()
        {
            RotateCurrentFurniture();
        }
    }
}