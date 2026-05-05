using Project.Input;
using Project.Interfaces;
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
        private PlacementManager placementManager;
        private IInteractable currentInteractable;

        public IInteractable CurrentInteractable => currentInteractable;

        private void Awake()
        {
            input = GetComponent<PlayerInputHandler>();

            if (interactionOrigin == null)
            {
                Debug.LogError($"{nameof(PlayerInteractionController)} on {name} is missing Interaction Origin.");
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
            UpdateCurrentInteractable();
        }

        public void Initialize(UIManager newUIManager, PlacementManager newPlacementManager)
        {
            uiManager = newUIManager;
            placementManager = newPlacementManager;

            UpdateReticleState();
        }

        private void UpdateCurrentInteractable()
        {
            IInteractable previousInteractable = currentInteractable;
            currentInteractable = null;

            if (placementManager != null && placementManager.IsPlacing)
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
            if (placementManager != null && placementManager.IsPlacing)
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