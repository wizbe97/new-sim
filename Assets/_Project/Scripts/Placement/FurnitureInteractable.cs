using Project.Interfaces;
using Project.Managers;
using UnityEngine;

namespace Project.Placement
{
    [RequireComponent(typeof(FurnitureItem))]
    public sealed class FurnitureInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionPrompt = "Pick up Furniture";

        private FurnitureItem furnitureItem;
        private PlacementManager placementManager;

        public string InteractionPrompt => interactionPrompt;

        public bool CanInteract =>
            placementManager != null &&
            !placementManager.IsPlacing;

        private void Awake()
        {
            furnitureItem = GetComponent<FurnitureItem>();
        }

        private void Start()
        {
            placementManager = FindFirstObjectByType<PlacementManager>();

            if (placementManager == null)
            {
                Debug.LogError($"{nameof(FurnitureInteractable)} could not find PlacementManager.");
            }
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            placementManager.BeginPlacement(furnitureItem);
        }
    }
}