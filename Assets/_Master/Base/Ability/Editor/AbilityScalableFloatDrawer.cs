using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using _Master.Base.Ability;

namespace _Master.Base.Ability.Editor
{
    [CustomPropertyDrawer(typeof(AbilityScalableFloat))]
    public class AbilityScalableFloatDrawer : PropertyDrawer
    {
        private static float LineHeight => EditorGUIUtility.singleLineHeight;
        private static float VerticalSpacing => EditorGUIUtility.standardVerticalSpacing;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lines = 1; // foldout line

            if (property.isExpanded)
            {
                lines += 1; // scalingMode

                SerializedProperty scalingModeProp = property.FindPropertyRelative("scalingMode");
                var mode = (AbilityScalableFloat.ScalingMode)scalingModeProp.enumValueIndex;

                if (mode == AbilityScalableFloat.ScalingMode.FlatValue)
                {
                    lines += 1; // flatValue
                }
                else
                {
                    lines += 1; // csvAsset
                    lines += 1; // csvColumn
                    lines += 1; // previewLevel
                    lines += 1; // preview value
                    lines += 1; // rebuild button
                }
            }

            return (LineHeight + VerticalSpacing) * lines;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect lineRect = new Rect(position.x, position.y, position.width, LineHeight);
            property.isExpanded = EditorGUI.Foldout(lineRect, property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            lineRect.y += LineHeight + VerticalSpacing;

            SerializedProperty scalingModeProp = property.FindPropertyRelative("scalingMode");
            SerializedProperty flatValueProp = property.FindPropertyRelative("flatValue");
            SerializedProperty csvAssetProp = property.FindPropertyRelative("csvAsset");
            SerializedProperty csvColumnProp = property.FindPropertyRelative("csvColumn");
            SerializedProperty previewLevelProp = property.FindPropertyRelative("previewLevel");

            EditorGUI.PropertyField(lineRect, scalingModeProp);

            var mode = (AbilityScalableFloat.ScalingMode)scalingModeProp.enumValueIndex;

            if (mode == AbilityScalableFloat.ScalingMode.FlatValue)
            {
                lineRect.y += LineHeight + VerticalSpacing;
                EditorGUI.PropertyField(lineRect, flatValueProp);
            }
            else
            {
                lineRect.y += LineHeight + VerticalSpacing;
                EditorGUI.PropertyField(lineRect, csvAssetProp, new GUIContent("CSV Asset"));

                lineRect.y += LineHeight + VerticalSpacing;
                DrawCsvColumnPopup(lineRect, property, csvAssetProp, csvColumnProp);

                lineRect.y += LineHeight + VerticalSpacing;
                EditorGUI.PropertyField(lineRect, previewLevelProp, new GUIContent("Preview Level"));

                lineRect.y += LineHeight + VerticalSpacing;
                DrawPreviewValue(lineRect, property, csvColumnProp);

                lineRect.y += LineHeight + VerticalSpacing;
                DrawRebuildButton(lineRect, property);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private void DrawCsvColumnPopup(Rect rect, SerializedProperty property, SerializedProperty csvAssetProp, SerializedProperty csvColumnProp)
        {
            TextAsset csvAsset = csvAssetProp.objectReferenceValue as TextAsset;
            string[] columns = Array.Empty<string>();

            if (csvAsset != null)
            {
                columns = CsvCurveTable.GetHeaders(csvAsset.text)
                    .Skip(1)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToArray();
            }

            if (columns.Length == 0)
            {
                EditorGUI.LabelField(rect, "CSV Column", "No columns found");
                return;
            }

            int currentIndex = Array.IndexOf(columns, csvColumnProp.stringValue);
            if (currentIndex < 0) currentIndex = 0;

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect valueRect = EditorGUI.PrefixLabel(rect, new GUIContent("CSV Column"));
            int newIndex = EditorGUI.Popup(valueRect, currentIndex, columns);

            EditorGUI.indentLevel = indent;
            if (newIndex >= 0 && newIndex < columns.Length)
            {
                csvColumnProp.stringValue = columns[newIndex];
            }
        }

        private void DrawPreviewValue(Rect rect, SerializedProperty property, SerializedProperty csvColumnProp)
        {
            AbilityScalableFloat instance = GetInstance(property);
            if (instance == null)
            {
                EditorGUI.LabelField(rect, "Preview", "No instance");
                return;
            }

            float previewValue = instance.GetPreviewValue();
            string column = string.IsNullOrEmpty(csvColumnProp.stringValue) ? "(none)" : csvColumnProp.stringValue;
            EditorGUI.LabelField(rect, $"Preview ({column})", previewValue.ToString("F4"));
        }

        private void DrawRebuildButton(Rect rect, SerializedProperty property)
        {
            if (GUI.Button(rect, "Rebuild Curve From CSV"))
            {
                AbilityScalableFloat instance = GetInstance(property);
                if (instance != null)
                {
                    instance.RebuildCurveFromCsv();
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }
            }
        }

        private AbilityScalableFloat GetInstance(SerializedProperty property)
        {
            object target = property.serializedObject.targetObject;
            if (target == null)
                return null;

            return fieldInfo.GetValue(target) as AbilityScalableFloat;
        }
    }
}