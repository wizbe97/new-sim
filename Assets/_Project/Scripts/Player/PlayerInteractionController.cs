using Project.Input;
using Project.Interfaces;
using Project.Placement;
using Project.Managers;
using UnityEngine;

namespace Project.Player
{
    [RequireComponent(typeof(PlayerInputHandler))]
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform interactionOrigin;

        [Header("Interaction")]
        [SerializeField, Min(0.1f)] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionLayerMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Debug")]
        [SerializeField] private bool drawDebugRay = true;

        private PlayerInputHandler input;
        private UIManager uiManager;
        private BuildingManager buildingManager;
        private IInteractable currentInteractable;
        private ISecondaryInteractable currentSecondaryInteractable;

        private bool canInteract = true;

        public IInteractable CurrentInteractable => currentInteractable;
        public ISecondaryInteractable CurrentSecondaryInteractable => currentSecondaryInteractable;
        public bool CanInteract => canInteract;

        private void Awake()
        {
            input = GetComponent<PlayerInputHandler>();

            if (interactionOrigin == null)
            {
                Debug.LogError($"{nameof(PlayerInteractionController)} on {name} is missing Interaction Origin.", this);
            }
        }

        private void OnEnable()
        {
            input.InteractPressed += HandleInteractPressed;
            input.SecondaryInteractPressed += HandleSecondaryInteractPressed;
        }

        private void OnDisable()
        {
            input.InteractPressed -= HandleInteractPressed;
            input.SecondaryInteractPressed -= HandleSecondaryInteractPressed;
        }

        private void Update()
        {
            if (!canInteract)
            {
                ClearCurrentInteractables();
                return;
            }

            UpdateCurrentInteractables();
        }

        public void Initialize(UIManager newUIManager, BuildingManager newBuildingManager)
        {
            uiManager = newUIManager;
            buildingManager = newBuildingManager;

            UpdateReticleState();
        }

        public void SetCanInteract(bool value)
        {
            canInteract = value;

            if (!canInteract)
            {
                ClearCurrentInteractables();
            }
        }

        private void UpdateCurrentInteractables()
        {
            IInteractable previousInteractable = currentInteractable;
            ISecondaryInteractable previousSecondaryInteractable = currentSecondaryInteractable;

            currentInteractable = null;
            currentSecondaryInteractable = null;

            if (buildingManager != null && buildingManager.IsBuilding)
            {
                UpdateReticleStateIfChanged(previousInteractable, previousSecondaryInteractable);
                return;
            }

            if (interactionOrigin == null)
            {
                UpdateReticleStateIfChanged(previousInteractable, previousSecondaryInteractable);
                return;
            }

            Ray ray = new Ray(interactionOrigin.position, interactionOrigin.forward);

            if (drawDebugRay)
            {
                Debug.DrawRay(
                    ray.origin,
                    ray.direction * interactionDistance,
                    Color.cyan
                );
            }

            if (Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    interactionDistance,
                    interactionLayerMask,
                    triggerInteraction))
            {
                ResolvePrimaryInteractable(hit);
                ResolveSecondaryInteractable(hit);
            }

            UpdateReticleStateIfChanged(previousInteractable, previousSecondaryInteractable);
        }

        private void ResolvePrimaryInteractable(RaycastHit hit)
        {
            if (!hit.collider.TryGetComponent(out IInteractable interactable))
            {
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (interactable != null && interactable.CanInteract)
            {
                currentInteractable = interactable;
            }
        }

        private void ResolveSecondaryInteractable(RaycastHit hit)
        {
            if (!hit.collider.TryGetComponent(out ISecondaryInteractable secondaryInteractable))
            {
                secondaryInteractable = hit.collider.GetComponentInParent<ISecondaryInteractable>();
            }

            if (secondaryInteractable != null && secondaryInteractable.CanSecondaryInteract)
            {
                currentSecondaryInteractable = secondaryInteractable;
            }
        }

        private void ClearCurrentInteractables()
        {
            if (currentInteractable == null && currentSecondaryInteractable == null)
            {
                return;
            }

            currentInteractable = null;
            currentSecondaryInteractable = null;
            UpdateReticleState();
        }

        private void UpdateReticleStateIfChanged(
            IInteractable previousInteractable,
            ISecondaryInteractable previousSecondaryInteractable)
        {
            if (ReferenceEquals(previousInteractable, currentInteractable) &&
                ReferenceEquals(previousSecondaryInteractable, currentSecondaryInteractable))
            {
                return;
            }

            UpdateReticleState();
        }

        private void UpdateReticleState()
        {
            if (uiManager == null)
            {
                return;
            }

            uiManager.SetReticleInteractableState(
                currentInteractable != null ||
                currentSecondaryInteractable != null);
        }

        private void HandleInteractPressed()
        {
            if (!canInteract)
            {
                return;
            }

            if (buildingManager != null && buildingManager.IsBuilding)
            {
                return;
            }

            if (currentInteractable == null)
            {
                return;
            }

            currentInteractable.Interact();
        }

        private void HandleSecondaryInteractPressed()
        {
            if (!canInteract)
            {
                return;
            }

            if (buildingManager != null && buildingManager.IsBuilding)
            {
                return;
            }

            if (currentSecondaryInteractable == null)
            {
                return;
            }

            currentSecondaryInteractable.SecondaryInteract();
        }
    }
}