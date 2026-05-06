using Project.Hands;
using Project.Interfaces;
using UnityEngine;

namespace Project.Placement
{
    [RequireComponent(typeof(FurnitureItem))]
    public sealed class FurnitureInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionPrompt = "Pick up Furniture";

        private FurnitureItem furnitureItem;
        private InHandManager inHandManager;

        public string InteractionPrompt => interactionPrompt;

        public bool CanInteract =>
            furnitureItem != null &&
            inHandManager != null &&
            !inHandManager.IsHoldingSomething;

        private void Awake()
        {
            furnitureItem = GetComponent<FurnitureItem>();
        }

        private void Start()
        {
            inHandManager = FindFirstObjectByType<InHandManager>();

            if (inHandManager == null)
            {
                Debug.LogError($"{nameof(FurnitureInteractable)} could not find {nameof(InHandManager)}.", this);
            }
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            inHandManager.TryPickupFurniture(furnitureItem);
        }
    }
}