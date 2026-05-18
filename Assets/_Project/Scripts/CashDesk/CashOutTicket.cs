using Project.Interfaces;
using Project.Managers;
using Project.NPC.Customer;
using TMPro;
using UnityEngine;

namespace Project.CashDesk
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CashOutTicket : MonoBehaviour, IInteractable
    {
        [Header("Runtime")]
        [SerializeField] private int amount;
        [SerializeField] private bool isPlacedOnDesk;
        [SerializeField] private bool isPaid;
        [SerializeField] private float placedOnDeskTime = -1f;

        [Header("Visual")]
        [SerializeField] private Transform ticketVisual;
        [SerializeField] private Vector3 visualLocalScale = new Vector3(0.35f, 0.02f, 0.22f);
        [SerializeField] private Color visualColour = new Color(0.95f, 0.9f, 0.75f, 1f);

        [Header("Interaction")]
        [Tooltip("Collider thickness multiplier. X/Z match the ticket. Y can be slightly thicker so the raycast can reliably hit the paper.")]
        [SerializeField, Min(1f)] private float colliderHeightMultiplier = 2f;

        [Header("Desk Placement")]
        [Tooltip("Local rotation used when the ticket is placed on the desk. Y -90 is 90 degrees anticlockwise for most top-down desk setups.")]
        [SerializeField] private Vector3 deskLocalEulerAngles = new Vector3(0f, -90f, 0f);

        [Header("Ticket Value Text")]
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Color valueTextColour = Color.black;
        [SerializeField] private float valueTextFontSize = 4f;
        [SerializeField] private Vector3 valueTextLocalPosition = new Vector3(0f, 0.035f, 0f);
        [SerializeField] private Vector3 valueTextLocalEulerAngles = new Vector3(90f, 0f, 0f);
        [SerializeField] private Vector3 valueTextLocalScale = new Vector3(0.08f, 0.08f, 0.08f);

        private CustomerController owner;
        private CashDeskManager cashDesk;
        private BoxCollider boxCollider;

        public int Amount => amount;
        public CustomerController Owner => owner;
        public bool IsPlacedOnDesk => isPlacedOnDesk;
        public bool IsPaid => isPaid;
        public float PlacedOnDeskTime => placedOnDeskTime;

        public string InteractionPrompt => $"Pay ticket: £{amount}";

        public bool CanInteract
        {
            get
            {
                if (!isPlacedOnDesk)
                {
                    return false;
                }

                if (isPaid)
                {
                    return false;
                }

                if (cashDesk == null)
                {
                    return false;
                }

                return cashDesk.IsFrontTicket(this);
            }
        }

        public static CashOutTicket Create(
            int ticketAmount,
            CustomerController ticketOwner,
            Transform parent)
        {
            GameObject ticketRoot = new GameObject($"CashOutTicket_£{ticketAmount}");
            ticketRoot.transform.SetParent(parent, false);
            ticketRoot.transform.localPosition = Vector3.zero;
            ticketRoot.transform.localRotation = Quaternion.identity;
            ticketRoot.transform.localScale = Vector3.one;

            CashOutTicket ticket = ticketRoot.AddComponent<CashOutTicket>();
            ticket.Initialize(ticketAmount, ticketOwner);

            return ticket;
        }

        public void Initialize(int ticketAmount, CustomerController ticketOwner)
        {
            amount = Mathf.Max(0, ticketAmount);
            owner = ticketOwner;
            isPlacedOnDesk = false;
            isPaid = false;
            placedOnDeskTime = -1f;

            CacheCollider();
            CreateVisualIfMissing();
            CreateValueTextIfMissing();

            ConfigureColliderToMatchTicket();
            RefreshValueText();
        }

        public void AssignCashDesk(CashDeskManager newCashDesk)
        {
            cashDesk = newCashDesk;
        }

        public void AttachToHand(Transform handPoint)
        {
            if (handPoint == null)
            {
                return;
            }

            isPlacedOnDesk = false;
            placedOnDeskTime = -1f;

            transform.SetParent(handPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            ConfigureColliderToMatchTicket();
            RefreshValueText();
        }

        public void PlaceOnDesk(Transform deskPoint)
        {
            if (deskPoint == null)
            {
                return;
            }

            isPlacedOnDesk = true;
            placedOnDeskTime = Time.time;

            transform.SetParent(deskPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(deskLocalEulerAngles);
            transform.localScale = Vector3.one;

            ConfigureColliderToMatchTicket();
            RefreshValueText();

            Debug.Log(
                $"{name} placed on desk at {placedOnDeskTime:0.00}. CanInteract: {CanInteract}, Amount: £{amount}.",
                this);
        }

        public void MarkPaid()
        {
            isPaid = true;
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                Debug.LogWarning(
                    $"{name} cannot be paid. IsPlacedOnDesk: {isPlacedOnDesk}, IsPaid: {isPaid}, HasCashDesk: {cashDesk != null}, IsFrontTicket: {(cashDesk != null && cashDesk.IsFrontTicket(this))}.",
                    this);

                return;
            }

            cashDesk.TryPayTicket(this);
        }

        private void CacheCollider()
        {
            if (boxCollider != null)
            {
                return;
            }

            boxCollider = GetComponent<BoxCollider>();

            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
            }
        }

        private void ConfigureColliderToMatchTicket()
        {
            CacheCollider();

            boxCollider.isTrigger = false;
            boxCollider.center = Vector3.zero;

            boxCollider.size = new Vector3(
                visualLocalScale.x,
                Mathf.Max(visualLocalScale.y * colliderHeightMultiplier, visualLocalScale.y),
                visualLocalScale.z);
        }

        private void CreateVisualIfMissing()
        {
            if (ticketVisual != null)
            {
                return;
            }

            GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualObject.name = "TicketVisual";
            visualObject.transform.SetParent(transform, false);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
            visualObject.transform.localScale = visualLocalScale;

            Collider visualCollider = visualObject.GetComponent<Collider>();

            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }

            Renderer renderer = visualObject.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material.color = visualColour;
            }

            ticketVisual = visualObject.transform;
        }

        private void CreateValueTextIfMissing()
        {
            if (valueText != null)
            {
                return;
            }

            GameObject textObject = new GameObject("TicketValueText");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = valueTextLocalPosition;
            textObject.transform.localRotation = Quaternion.Euler(valueTextLocalEulerAngles);
            textObject.transform.localScale = valueTextLocalScale;

            TextMeshPro text = textObject.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = valueTextFontSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.richText = false;
            text.rectTransform.sizeDelta = new Vector2(8f, 2f);

            valueText = text;
        }

        private void RefreshValueText()
        {
            if (valueText == null)
            {
                return;
            }

            valueText.color = valueTextColour;
            valueText.text = $"£{amount}";
        }
    }
}