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

        private bool canInteract = true;

        public IInteractable CurrentInteractable => currentInteractable;
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
        }

        private void OnDisable()
        {
            input.InteractPressed -= HandleInteractPressed;
        }

        private void Update()
        {
            if (!canInteract)
            {
                ClearCurrentInteractable();
                return;
            }

            UpdateCurrentInteractable();
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
                ClearCurrentInteractable();
            }
        }

        private void UpdateCurrentInteractable()
        {
            IInteractable previousInteractable = currentInteractable;
            currentInteractable = null;

            if (buildingManager != null && buildingManager.IsBuilding)
            {
                UpdateReticleStateIfChanged(previousInteractable);
                return;
            }

            if (interactionOrigin == null)
            {
                UpdateReticleStateIfChanged(previousInteractable);
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
                if (!hit.collider.TryGetComponent(out IInteractable interactable))
                {
                    interactable = hit.collider.GetComponentInParent<IInteractable>();
                }

                if (interactable != null && interactable.CanInteract)
                {
                    currentInteractable = interactable;
                }
            }

            UpdateReticleStateIfChanged(previousInteractable);
        }

        private void ClearCurrentInteractable()
        {
            if (currentInteractable == null)
            {
                return;
            }

            currentInteractable = null;
            UpdateReticleState();
        }

        private void UpdateReticleStateIfChanged(IInteractable previousInteractable)
        {
            if (ReferenceEquals(previousInteractable, currentInteractable))
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

            uiManager.SetReticleInteractableState(currentInteractable != null);
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
    }
}