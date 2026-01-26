using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GAS.Editor
{
    [CustomPropertyDrawer(typeof(ScalableFloat))]
    public class ScalableFloatDrawer : PropertyDrawer
    {
        private static float LineHeight => EditorGUIUtility.singleLineHeight;
        private static float VerticalSpacing => EditorGUIUtility.standardVerticalSpacing;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lines = 1; // foldout line

            if (property.isExpanded)
            {
                lines += 1; // scalingMode
                lines += 1; // flatValue (always visible)

                SerializedProperty scalingModeProp = property.FindPropertyRelative("scalingMode");
                var mode = (ScalableFloat.ScalingMode)scalingModeProp.enumValueIndex;

                if (mode == ScalableFloat.ScalingMode.Curve)
                {
                    lines += 1; // csvAsset
                    lines += 1; // csvColumn
                    lines += 1; // previewLevel
                    lines += 1; // preview value
                    lines += 1; // rebuild button
                }
                else if (mode == ScalableFloat.ScalingMode.Attribute)
                {
                    lines += 1; // attributeType
                    lines += 1; // preview value
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

            lineRect.y += LineHeight + VerticalSpacing;
            EditorGUI.PropertyField(lineRect, flatValueProp, new GUIContent("Base Value"));

            var mode = (ScalableFloat.ScalingMode)scalingModeProp.enumValueIndex;

            if (mode == ScalableFloat.ScalingMode.Curve)
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
            else if (mode == ScalableFloat.ScalingMode.Attribute)
            {
                SerializedProperty attributeTypeProp = property.FindPropertyRelative("attributeType");
                lineRect.y += LineHeight + VerticalSpacing;
                EditorGUI.PropertyField(lineRect, attributeTypeProp, new GUIContent("Attribute"));

                lineRect.y += LineHeight + VerticalSpacing;
                DrawPreviewValue(lineRect, property, null, attributeTypeProp);
            }

            // FlatValue mode only uses base value, so no extra fields after flatValue

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

        private void DrawPreviewValue(Rect rect, SerializedProperty property, SerializedProperty csvColumnProp, SerializedProperty attributeTypeProp = null)
        {
            ScalableFloat instance = GetInstance(property);
            if (instance == null)
            {
                EditorGUI.LabelField(rect, "Preview", "No instance");
                return;
            }

            float previewValue = instance.GetPreviewValue();
            if (attributeTypeProp != null)
            {
                string attributeName = attributeTypeProp.enumDisplayNames[attributeTypeProp.enumValueIndex];
                EditorGUI.LabelField(rect, $"Preview ({attributeName})", previewValue.ToString("F4"));
            }
            else
            {
                string column = csvColumnProp == null || string.IsNullOrEmpty(csvColumnProp.stringValue) ? "(none)" : csvColumnProp.stringValue;
                EditorGUI.LabelField(rect, $"Preview ({column})", previewValue.ToString("F4"));
            }
        }

        private void DrawRebuildButton(Rect rect, SerializedProperty property)
        {
            if (GUI.Button(rect, "Rebuild Curve From CSV"))
            {
                ScalableFloat instance = GetInstance(property);
                if (instance != null)
                {
                    instance.RebuildCurveFromCsv();
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }
            }
        }

        private ScalableFloat GetInstance(SerializedProperty property)
        {
            object target = property.serializedObject.targetObject;
            if (target == null)
                return null;

            // Traverse the property path to get the actual instance
            string[] pathParts = property.propertyPath.Replace(".Array.data[", "[").Split('.');
            
            foreach (string part in pathParts)
            {
                if (target == null)
                    return null;

                if (part.Contains("["))
                {
                    // Handle array element access
                    string fieldName = part.Substring(0, part.IndexOf('['));
                    int index = int.Parse(part.Substring(part.IndexOf('[') + 1, part.IndexOf(']') - part.IndexOf('[') - 1));
                    
                    var field = target.GetType().GetField(fieldName, 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (field != null)
                    {
                        var array = field.GetValue(target) as System.Collections.IList;
                        if (array != null && index >= 0 && index < array.Count)
                        {
                            target = array[index];
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    // Normal field access
                    var field = target.GetType().GetField(part, 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (field != null)
                    {
                        target = field.GetValue(target);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return target as ScalableFloat;
        }
    }
}