using UnityEngine;

namespace Project.Shop
{
    [RequireComponent(typeof(DeliveryBox))]
    public sealed class DeliveryBoxInteractable : MonoBehaviour
    {
        private DeliveryBox deliveryBox;

        private void Awake()
        {
            deliveryBox = GetComponent<DeliveryBox>();
        }

        public void Open()
        {
            if (deliveryBox == null)
            {
                Debug.LogError($"{nameof(DeliveryBoxInteractable)} on {name} is missing DeliveryBox.", this);
                return;
            }

            deliveryBox.OpenBox();
        }
    }
}