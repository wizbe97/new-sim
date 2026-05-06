using Project.Hands;
using Project.Interfaces;
using UnityEngine;

namespace Project.Shop
{
    [RequireComponent(typeof(DeliveryBox))]
    public sealed class DeliveryBoxInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionPrompt = "Pick up Box";

        private DeliveryBox deliveryBox;
        private InHandManager inHandManager;

        public string InteractionPrompt => interactionPrompt;

        public bool CanInteract =>
            deliveryBox != null &&
            deliveryBox.CanCarry &&
            inHandManager != null &&
            !inHandManager.IsHoldingSomething;

        private void Awake()
        {
            deliveryBox = GetComponent<DeliveryBox>();
        }

        private void Start()
        {
            inHandManager = FindFirstObjectByType<InHandManager>();

            if (inHandManager == null)
            {
                Debug.LogError($"{nameof(DeliveryBoxInteractable)} could not find {nameof(InHandManager)}.", this);
            }
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            inHandManager.TryPickupBox(deliveryBox);
        }
    }
}