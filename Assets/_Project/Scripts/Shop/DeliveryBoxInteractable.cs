using Project.Interfaces;
using Project.Managers;
using UnityEngine;

namespace Project.Shop
{
    [RequireComponent(typeof(DeliveryBox))]
    public sealed class DeliveryBoxInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionPrompt = "Pick up Box";

        private DeliveryBox deliveryBox;
        private BoxCarryManager boxCarryManager;

        public string InteractionPrompt => interactionPrompt;

        public bool CanInteract =>
            deliveryBox != null &&
            !deliveryBox.HasBeenOpened &&
            boxCarryManager != null &&
            !boxCarryManager.IsHoldingBox;

        private void Awake()
        {
            deliveryBox = GetComponent<DeliveryBox>();
        }

        private void Start()
        {
            boxCarryManager = FindFirstObjectByType<BoxCarryManager>();

            if (boxCarryManager == null)
            {
                Debug.LogError($"{nameof(DeliveryBoxInteractable)} could not find {nameof(BoxCarryManager)}.", this);
            }
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            boxCarryManager.BeginCarry(deliveryBox);
        }
    }
}