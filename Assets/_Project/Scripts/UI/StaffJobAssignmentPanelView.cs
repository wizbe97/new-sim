using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Staff.UI
{
    public sealed class StaffJobAssignmentPanelView : MonoBehaviour
    {
        private Canvas canvas;
        private TextMeshProUGUI titleText;
        private Button idleButton;
        private Button slotCollectionButton;
        private Button cashDeskButton;
        private Button closeButton;

        public event Action<StaffJobCategory> JobSelected;
        public event Action CloseRequested;

        public static StaffJobAssignmentPanelView Create()
        {
            GameObject root = new GameObject("StaffJobAssignmentPanel");
            StaffJobAssignmentPanelView view = root.AddComponent<StaffJobAssignmentPanelView>();
            view.BuildUi();
            view.Hide();

            return view;
        }

        public void Show(StaffMember staffMember)
        {
            if (staffMember == null)
            {
                Hide();
                return;
            }

            titleText.text = $"Assign Job: {staffMember.StaffName}";
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
            canvas.sortingOrder = 120;

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
            panelRect.sizeDelta = new Vector2(420f, 360f);
            panelRect.anchoredPosition = Vector2.zero;

            titleText = CreateText(
                "TitleText",
                panelObject.transform,
                "Assign Job",
                26,
                TextAlignmentOptions.Center);

            SetRect(titleText.rectTransform, new Vector2(0f, 130f), new Vector2(380f, 44f));

            idleButton = CreateButton(panelObject.transform, "IdleButton", "Idle");
            SetRect(idleButton.GetComponent<RectTransform>(), new Vector2(0f, 65f), new Vector2(280f, 46f));

            slotCollectionButton = CreateButton(panelObject.transform, "SlotCollectionButton", "Slot Collection");
            SetRect(slotCollectionButton.GetComponent<RectTransform>(), new Vector2(0f, 5f), new Vector2(280f, 46f));

            cashDeskButton = CreateButton(panelObject.transform, "CashDeskButton", "Cash Desk");
            SetRect(cashDeskButton.GetComponent<RectTransform>(), new Vector2(0f, -55f), new Vector2(280f, 46f));

            closeButton = CreateButton(panelObject.transform, "CloseButton", "Close");
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0f, -130f), new Vector2(180f, 42f));

            idleButton.onClick.AddListener(() => HandleJobClicked(StaffJobCategory.Idle));
            slotCollectionButton.onClick.AddListener(() => HandleJobClicked(StaffJobCategory.SlotCollection));
            cashDeskButton.onClick.AddListener(() => HandleJobClicked(StaffJobCategory.CashDesk));
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        private GameObject CreateUiObject(string objectName, Transform parent)
        {
            GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private TextMeshProUGUI CreateText(
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

            TextMeshProUGUI buttonText = CreateText(
                "Text",
                buttonObject.transform,
                label,
                20,
                TextAlignmentOptions.Center);

            RectTransform textRect = buttonText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private void SetRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

        private void HandleJobClicked(StaffJobCategory jobCategory)
        {
            JobSelected?.Invoke(jobCategory);
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (idleButton != null)
            {
                idleButton.onClick.RemoveAllListeners();
            }

            if (slotCollectionButton != null)
            {
                slotCollectionButton.onClick.RemoveAllListeners();
            }

            if (cashDeskButton != null)
            {
                cashDeskButton.onClick.RemoveAllListeners();
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
            }
        }
    }
}