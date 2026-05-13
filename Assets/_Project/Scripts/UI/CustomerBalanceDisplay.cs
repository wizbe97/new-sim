using System.Collections;
using TMPro;
using UnityEngine;

namespace Project.NPC.Customer.UI
{
    public sealed class CustomerBalanceDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CustomerController customer;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text paymentPopupText;

        [Header("Colours")]
        [SerializeField] private Color balanceColour = new Color(0.2f, 0.65f, 1f, 1f);
        [SerializeField] private Color paymentPopupColour = new Color(0.2f, 0.65f, 1f, 1f);

        [Header("Popup")]
        [SerializeField, Min(0.1f)] private float popupDurationSeconds = 1f;

        private Coroutine popupRoutine;

        private void Awake()
        {
            if (customer == null)
            {
                customer = GetComponentInParent<CustomerController>();
            }

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

            if (balanceText == null && texts.Length > 0)
            {
                balanceText = texts[0];
            }

            if (paymentPopupText == null && texts.Length > 1)
            {
                paymentPopupText = texts[1];
            }
        }

        private void OnEnable()
        {
            if (customer != null)
            {
                customer.CustomerMoneyChanged += HandleCustomerMoneyChanged;
                customer.CashOutPaymentReceived += HandleCashOutPaymentReceived;
            }

            HidePopup();
            RefreshBalance();
        }

        private void OnDisable()
        {
            if (customer != null)
            {
                customer.CustomerMoneyChanged -= HandleCustomerMoneyChanged;
                customer.CashOutPaymentReceived -= HandleCashOutPaymentReceived;
            }
        }

        private void HandleCustomerMoneyChanged(CustomerController changedCustomer)
        {
            RefreshBalance();
        }

        private void HandleCashOutPaymentReceived(int amount)
        {
            RefreshBalance();
            ShowPaymentPopup(amount);
        }

        private void RefreshBalance()
        {
            if (balanceText == null || customer == null || customer.Stats == null)
            {
                return;
            }

            balanceText.color = balanceColour;
            balanceText.text = $"£{customer.Stats.WalletBalance}";
        }

        private void ShowPaymentPopup(int amount)
        {
            if (paymentPopupText == null)
            {
                return;
            }

            if (popupRoutine != null)
            {
                StopCoroutine(popupRoutine);
            }

            popupRoutine = StartCoroutine(PopupRoutine(amount));
        }

        private IEnumerator PopupRoutine(int amount)
        {
            paymentPopupText.gameObject.SetActive(true);
            paymentPopupText.text = $"+£{amount}";

            float elapsed = 0f;

            while (elapsed < popupDurationSeconds)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / popupDurationSeconds);

                Color colour = paymentPopupColour;
                colour.a = alpha;

                paymentPopupText.color = colour;

                yield return null;
            }

            HidePopup();
        }

        private void HidePopup()
        {
            if (paymentPopupText == null)
            {
                return;
            }

            paymentPopupText.gameObject.SetActive(false);
        }
    }
}