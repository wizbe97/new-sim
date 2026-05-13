using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.SlotMachines.UI
{
    public sealed class SlotMachineCollectionPanelView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private Slider amountSlider;
        [SerializeField] private TMP_InputField amountInput;
        [SerializeField] private Button collectButton;
        [SerializeField] private Button closeButton;

        private int maximumAmount;
        private bool isUpdating;

        public event Action<int> CollectRequested;
        public event Action CloseRequested;

        public static SlotMachineCollectionPanelView Create()
        {
            GameObject root = new GameObject("SlotMachineCollectionPanel");
            SlotMachineCollectionPanelView view = root.AddComponent<SlotMachineCollectionPanelView>();
            view.BuildUi();
            view.Hide();

            return view;
        }

        public void Show(SlotMachine slotMachine)
        {
            if (slotMachine == null)
            {
                Hide();
                return;
            }

            maximumAmount = Mathf.Max(0, slotMachine.StoredCashFromDeposits);

            titleText.text = slotMachine.name;
            balanceText.text =
                $"Stored Cash: £{slotMachine.StoredCashFromDeposits}\n" +
                $"Current Customer Credit: £{slotMachine.CurrentSessionCredit}";

            SetAmount(maximumAmount);

            collectButton.interactable = maximumAmount > 0;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void BuildUi()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            RectTransform rootRect = gameObject.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject panelObject = CreateUiObject("Panel", transform);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(460f, 280f);
            panelRect.anchoredPosition = Vector2.zero;

            titleText = CreateText("TitleText", panelObject.transform, "Slot Machine", 26, TextAlignmentOptions.Center);
            SetRect(titleText.rectTransform, new Vector2(0f, 105f), new Vector2(400f, 40f));

            balanceText = CreateText("BalanceText", panelObject.transform, "Stored Cash: £0", 20, TextAlignmentOptions.Center);
            SetRect(balanceText.rectTransform, new Vector2(0f, 55f), new Vector2(420f, 55f));

            amountSlider = CreateSlider(panelObject.transform);
            SetRect(amountSlider.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(350f, 30f));

            amountInput = CreateInputField(panelObject.transform);
            SetRect(amountInput.GetComponent<RectTransform>(), new Vector2(0f, -45f), new Vector2(180f, 38f));

            collectButton = CreateButton(panelObject.transform, "CollectButton", "Collect");
            SetRect(collectButton.GetComponent<RectTransform>(), new Vector2(-80f, -100f), new Vector2(150f, 42f));

            closeButton = CreateButton(panelObject.transform, "CloseButton", "Close");
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(80f, -100f), new Vector2(150f, 42f));

            amountSlider.wholeNumbers = true;
            amountSlider.minValue = 0f;
            amountSlider.onValueChanged.AddListener(HandleSliderChanged);

            amountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            amountInput.onValueChanged.AddListener(HandleInputChanged);

            collectButton.onClick.AddListener(HandleCollectClicked);
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        private GameObject CreateUiObject(string objectName, Transform parent)
        {
            GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private TMP_Text CreateText(
            string objectName,
            Transform parent,
            string text,
            int fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateUiObject(objectName, parent);
            TextMeshProUGUI tmpText = textObject.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.alignment = alignment;
            tmpText.color = Color.white;
            tmpText.raycastTarget = false;
            return tmpText;
        }

        private Button CreateButton(Transform parent, string objectName, string label)
        {
            GameObject buttonObject = CreateUiObject(objectName, parent);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.18f, 0.18f, 1f);

            Button button = buttonObject.AddComponent<Button>();

            TMP_Text buttonText = CreateText("Text", buttonObject.transform, label, 20, TextAlignmentOptions.Center);
            SetRect(buttonText.rectTransform, Vector2.zero, Vector2.zero);
            buttonText.rectTransform.anchorMin = Vector2.zero;
            buttonText.rectTransform.anchorMax = Vector2.one;
            buttonText.rectTransform.offsetMin = Vector2.zero;
            buttonText.rectTransform.offsetMax = Vector2.zero;

            return button;
        }

        private Slider CreateSlider(Transform parent)
        {
            GameObject sliderObject = CreateUiObject("AmountSlider", parent);
            Slider slider = sliderObject.AddComponent<Slider>();

            GameObject backgroundObject = CreateUiObject("Background", sliderObject.transform);
            Image backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.25f);
            backgroundRect.anchorMax = new Vector2(1f, 0.75f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject fillAreaObject = CreateUiObject("Fill Area", sliderObject.transform);
            RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-5f, 0f);

            GameObject fillObject = CreateUiObject("Fill", fillAreaObject.transform);
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = new Color(0.1f, 0.65f, 1f, 1f);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handleAreaObject = CreateUiObject("Handle Slide Area", sliderObject.transform);
            RectTransform handleAreaRect = handleAreaObject.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            GameObject handleObject = CreateUiObject("Handle", handleAreaObject.transform);
            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = Color.white;
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(22f, 22f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }

        private TMP_InputField CreateInputField(Transform parent)
        {
            GameObject inputObject = CreateUiObject("AmountInput", parent);

            Image image = inputObject.AddComponent<Image>();
            image.color = Color.white;

            TMP_InputField inputField = inputObject.AddComponent<TMP_InputField>();

            TMP_Text text = CreateText("Text", inputObject.transform, string.Empty, 20, TextAlignmentOptions.Center);
            text.color = Color.black;
            text.raycastTarget = true;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(8f, 0f);
            text.rectTransform.offsetMax = new Vector2(-8f, 0f);

            TMP_Text placeholder = CreateText("Placeholder", inputObject.transform, "0", 20, TextAlignmentOptions.Center);
            placeholder.color = new Color(0f, 0f, 0f, 0.4f);
            placeholder.rectTransform.anchorMin = Vector2.zero;
            placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = new Vector2(8f, 0f);
            placeholder.rectTransform.offsetMax = new Vector2(-8f, 0f);

            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.characterValidation = TMP_InputField.CharacterValidation.Integer;

            return inputField;
        }

        private void SetRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;

            if (size != Vector2.zero)
            {
                rectTransform.sizeDelta = size;
            }
        }

        private void SetAmount(int amount)
        {
            int clampedAmount = Mathf.Clamp(amount, 0, maximumAmount);

            isUpdating = true;

            amountSlider.maxValue = maximumAmount;
            amountSlider.value = clampedAmount;
            amountInput.text = clampedAmount.ToString();

            isUpdating = false;
        }

        private void HandleSliderChanged(float value)
        {
            if (isUpdating)
            {
                return;
            }

            SetAmount(Mathf.RoundToInt(value));
        }

        private void HandleInputChanged(string value)
        {
            if (isUpdating)
            {
                return;
            }

            if (!int.TryParse(value, out int parsedAmount))
            {
                parsedAmount = 0;
            }

            SetAmount(parsedAmount);
        }

        private void HandleCollectClicked()
        {
            if (!int.TryParse(amountInput.text, out int amount))
            {
                amount = maximumAmount;
            }

            CollectRequested?.Invoke(Mathf.Clamp(amount, 0, maximumAmount));
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }
    }
}