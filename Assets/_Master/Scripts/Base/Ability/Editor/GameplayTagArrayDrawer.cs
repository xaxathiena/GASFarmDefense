using UnityEditor;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Custom property drawer for GameplayTag arrays
    /// Provides a multi-select dropdown in the Unity Inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(GameplayTag[]))]
    public class GameplayTagArrayDrawer : PropertyDrawer
    {
        private const float LineHeight = 18f;
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return LineHeight;

            // Calculate height for expanded array
            int arraySize = property.arraySize;
            float height = LineHeight; // Foldout line
            height += LineHeight; // Size field
            height += arraySize * (LineHeight + Spacing); // Each element
            height += LineHeight + Spacing; // Add button space

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw foldout
            Rect foldoutRect = new Rect(position.x, position.y, position.width, LineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                // Draw size field
                Rect sizeRect = new Rect(position.x, position.y + LineHeight, position.width, LineHeight);
                int newSize = EditorGUI.IntField(sizeRect, "Size", property.arraySize);
                if (newSize != property.arraySize)
                {
                    property.arraySize = Mathf.Max(0, newSize);
                }

                // Draw each element with enum dropdown
                for (int i = 0; i < property.arraySize; i++)
                {
                    Rect elementRect = new Rect(
                        position.x,
                        position.y + LineHeight * (i + 2) + Spacing * (i + 1),
                        position.width,
                        LineHeight
                    );

                    SerializedProperty element = property.GetArrayElementAtIndex(i);
                    
                    // Draw enum popup with element label
                    EditorGUI.PropertyField(elementRect, element, new GUIContent($"Element {i}"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }

    /// <summary>
    /// Enhanced editor window for multi-selecting GameplayTags
    /// Can be opened from the context menu
    /// </summary>
    public class GameplayTagSelectorWindow : EditorWindow
    {
        private SerializedProperty targetProperty;
        private bool[] selectedTags;
        private Vector2 scrollPosition;

        public static void Show(SerializedProperty property)
        {
            var window = GetWindow<GameplayTagSelectorWindow>("Select Tags");
            window.targetProperty = property;
            window.InitializeSelection();
            window.Show();
        }

        private void InitializeSelection()
        {
            var allTags = System.Enum.GetValues(typeof(GameplayTag));
            selectedTags = new bool[allTags.Length];

            // Mark currently selected tags
            for (int i = 0; i < targetProperty.arraySize; i++)
            {
                var element = targetProperty.GetArrayElementAtIndex(i);
                int tagValue = element.intValue;
                if (tagValue < selectedTags.Length)
                {
                    selectedTags[tagValue] = true;
                }
            }
        }

        private void OnGUI()
        {
            if (targetProperty == null || !targetProperty.serializedObject.targetObject)
            {
                Close();
                return;
            }

            EditorGUILayout.LabelField("Select Gameplay Tags", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var allTags = System.Enum.GetValues(typeof(GameplayTag));
            var tagNames = System.Enum.GetNames(typeof(GameplayTag));

            for (int i = 0; i < allTags.Length; i++)
            {
                GameplayTag tag = (GameplayTag)allTags.GetValue(i);
                
                // Skip None
                if (tag == GameplayTag.None)
                    continue;

                EditorGUI.BeginChangeCheck();
                bool isSelected = selectedTags[i];
                isSelected = EditorGUILayout.ToggleLeft(tagNames[i].Replace("_", "."), isSelected);
                
                if (EditorGUI.EndChangeCheck())
                {
                    selectedTags[i] = isSelected;
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            
            if (GUILayout.Button("Apply"))
            {
                ApplySelection();
                Close();
            }
        }

        private void ApplySelection()
        {
            targetProperty.serializedObject.Update();

            // Clear array
            targetProperty.ClearArray();

            // Add selected tags
            var allTags = System.Enum.GetValues(typeof(GameplayTag));
            for (int i = 0; i < selectedTags.Length; i++)
            {
                if (selectedTags[i])
                {
                    int newIndex = targetProperty.arraySize;
                    targetProperty.InsertArrayElementAtIndex(newIndex);
                    targetProperty.GetArrayElementAtIndex(newIndex).intValue = i;
                }
            }

            targetProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}
