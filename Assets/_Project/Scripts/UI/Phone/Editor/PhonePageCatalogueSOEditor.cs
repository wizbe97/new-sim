#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Project.UI.Phone.Editor
{
    [CustomEditor(typeof(PhonePageCatalogueSO))]
    public sealed class PhonePageCatalogueSOEditor : UnityEditor.Editor
    {
        private SerializedProperty pageId;
        private SerializedProperty pageTitle;
        private SerializedProperty pageDescription;
        private SerializedProperty pageIcon;

        private SerializedProperty scrollWheelSensitivity;

        private SerializedProperty layoutMode;
        private SerializedProperty listSettings;
        private SerializedProperty gridSettings;

        private SerializedProperty items;

        private void OnEnable()
        {
            pageId = serializedObject.FindProperty("pageId");
            pageTitle = serializedObject.FindProperty("pageTitle");
            pageDescription = serializedObject.FindProperty("pageDescription");
            pageIcon = serializedObject.FindProperty("pageIcon");

            scrollWheelSensitivity = serializedObject.FindProperty("scrollWheelSensitivity");

            layoutMode = serializedObject.FindProperty("layoutMode");
            listSettings = serializedObject.FindProperty("listSettings");
            gridSettings = serializedObject.FindProperty("gridSettings");

            items = serializedObject.FindProperty("items");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPageIdentity();
            DrawScrolling();
            DrawLayout();
            DrawItems();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPageIdentity()
        {
            EditorGUILayout.LabelField("Page Identity", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(pageId);
            EditorGUILayout.PropertyField(pageTitle);
            EditorGUILayout.PropertyField(pageDescription);
            EditorGUILayout.PropertyField(pageIcon);

            EditorGUILayout.Space(10f);
        }

        private void DrawScrolling()
        {
            EditorGUILayout.LabelField("Scrolling", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(scrollWheelSensitivity);

            EditorGUILayout.Space(10f);
        }

        private void DrawLayout()
        {
            EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(layoutMode);

            PhonePageLayoutMode mode = (PhonePageLayoutMode)layoutMode.enumValueIndex;

            EditorGUI.indentLevel++;

            switch (mode)
            {
                case PhonePageLayoutMode.VerticalList:
                    EditorGUILayout.PropertyField(listSettings, includeChildren: true);
                    break;

                case PhonePageLayoutMode.IconGrid:
                    EditorGUILayout.PropertyField(gridSettings, includeChildren: true);
                    break;
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10f);
        }

        private void DrawItems()
        {
            EditorGUILayout.LabelField("Items", EditorStyles.boldLabel);

            if (items == null)
            {
                EditorGUILayout.HelpBox("Could not find items property.", MessageType.Error);
                return;
            }

            for (int i = 0; i < items.arraySize; i++)
            {
                SerializedProperty itemProperty = items.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();

                string itemTitle = GetItemHeader(itemProperty, i);
                itemProperty.isExpanded = EditorGUILayout.Foldout(
                    itemProperty.isExpanded,
                    itemTitle,
                    true);

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    RemoveItemAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUILayout.EndHorizontal();

                if (itemProperty.managedReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(
                        "This item is empty. Remove it and add a typed item using the buttons below.",
                        MessageType.Warning);
                }
                else if (itemProperty.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    DrawManagedReferenceChildren(itemProperty);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6f);

            DrawAddButtons();
        }

        private void DrawAddButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Page Link"))
            {
                AddItem(new PhonePageLinkItem());
            }

            if (GUILayout.Button("Add Shop Item"))
            {
                AddItem(new PhoneShopItem());
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Setting"))
            {
                AddItem(new PhoneSettingItem());
            }

            if (GUILayout.Button("Add Close Item"))
            {
                AddItem(new PhoneCloseItem());
            }

            EditorGUILayout.EndHorizontal();
        }

        private void AddItem(PhonePageItem item)
        {
            int index = items.arraySize;
            items.InsertArrayElementAtIndex(index);

            SerializedProperty newItemProperty = items.GetArrayElementAtIndex(index);
            newItemProperty.managedReferenceValue = item;
            newItemProperty.isExpanded = true;

            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(target);
        }

        private void RemoveItemAt(int index)
        {
            items.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(target);
        }

        private static void DrawManagedReferenceChildren(SerializedProperty property)
        {
            SerializedProperty childProperty = property.Copy();
            SerializedProperty endProperty = childProperty.GetEndProperty();

            bool enterChildren = true;

            while (childProperty.NextVisible(enterChildren))
            {
                if (SerializedProperty.EqualContents(childProperty, endProperty))
                {
                    break;
                }

                EditorGUILayout.PropertyField(childProperty, includeChildren: true);
                enterChildren = false;
            }
        }

        private static string GetItemHeader(SerializedProperty itemProperty, int index)
        {
            object value = itemProperty.managedReferenceValue;

            if (value == null)
            {
                return $"Element {index} - Empty";
            }

            if (value is PhonePageItem item)
            {
                string title = item.GetTitle();

                if (!string.IsNullOrWhiteSpace(title))
                {
                    return $"{index}: {value.GetType().Name} - {title}";
                }
            }

            return $"{index}: {value.GetType().Name}";
        }
    }
}
#endif