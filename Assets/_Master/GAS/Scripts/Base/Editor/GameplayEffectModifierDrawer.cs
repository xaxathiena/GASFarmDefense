using UnityEditor;
using UnityEngine;

namespace GAS.Editor
{
    /// <summary>
    /// Custom property drawer for GameplayEffectModifier with conditional layout
    /// based on calculation type (similar to Unreal's GAS editor)
    /// </summary>
    [CustomPropertyDrawer(typeof(GameplayEffectModifier))]
    public class GameplayEffectModifierDrawer : PropertyDrawer
    {
        private static float LineHeight => EditorGUIUtility.singleLineHeight;
        private static float VerticalSpacing => EditorGUIUtility.standardVerticalSpacing;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return LineHeight;

            float totalHeight = LineHeight + VerticalSpacing; // Foldout line

            // Target section
            totalHeight += LineHeight + VerticalSpacing; // Target header

            SerializedProperty attributeProp = property.FindPropertyRelative("attribute");
            totalHeight += EditorGUI.GetPropertyHeight(attributeProp, true) + VerticalSpacing;

            totalHeight += LineHeight + VerticalSpacing; // operation

            // Magnitude Calculation section
            totalHeight += LineHeight + VerticalSpacing; // header label
            totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("calculationType"), true) + VerticalSpacing * 3; // calculation type field

            SerializedProperty calculationTypeProp = property.FindPropertyRelative("calculationType");
            var calculationType = (EModifierCalculationType)calculationTypeProp.enumValueIndex;

            switch (calculationType)
            {
                case EModifierCalculationType.ScalableFloat:
                    SerializedProperty scalableMagnitudeProp = property.FindPropertyRelative("scalableMagnitude");
                    totalHeight += EditorGUI.GetPropertyHeight(scalableMagnitudeProp, true) + VerticalSpacing;
                    break;
                case EModifierCalculationType.AttributeBased:
                    SerializedProperty backingAttributeProp = property.FindPropertyRelative("backingAttribute");
                    totalHeight += EditorGUI.GetPropertyHeight(backingAttributeProp, true) + VerticalSpacing;
                    totalHeight += (LineHeight + VerticalSpacing) * 6; // source, snapshot, formula, coefficient, pre, post
                    break;
                case EModifierCalculationType.CustomCalculationClass:
                    SerializedProperty customCalculationProp = property.FindPropertyRelative("customCalculation");
                    totalHeight += EditorGUI.GetPropertyHeight(customCalculationProp, true) + VerticalSpacing;
                    SerializedProperty scalableMagnitudePropCC = property.FindPropertyRelative("scalableMagnitude");
                    totalHeight += EditorGUI.GetPropertyHeight(scalableMagnitudePropCC, true) + VerticalSpacing;
                    totalHeight += LineHeight * 2.5f + VerticalSpacing; // HelpBox
                    break;
                case EModifierCalculationType.SetByCaller:
                    SerializedProperty setByCallerTagProp = property.FindPropertyRelative("setByCallerTag");
                    totalHeight += EditorGUI.GetPropertyHeight(setByCallerTagProp, true) + VerticalSpacing;
                    break;
            }


            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect currentRect = new Rect(position.x, position.y, position.width, LineHeight);

            // Main foldout
            property.isExpanded = EditorGUI.Foldout(currentRect, property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            // Get all properties
            SerializedProperty attributeProp = property.FindPropertyRelative("attribute");
            SerializedProperty operationProp = property.FindPropertyRelative("operation");
            SerializedProperty calculationTypeProp = property.FindPropertyRelative("calculationType");
            SerializedProperty scalableMagnitudeProp = property.FindPropertyRelative("scalableMagnitude");
            SerializedProperty backingAttributeProp = property.FindPropertyRelative("backingAttribute");
            SerializedProperty attributeSourceProp = property.FindPropertyRelative("attributeSource");
            SerializedProperty snapshotAttributeProp = property.FindPropertyRelative("snapshotAttribute");
            SerializedProperty coefficientProp = property.FindPropertyRelative("coefficient");
            SerializedProperty preMultiplyAdditiveValueProp = property.FindPropertyRelative("preMultiplyAdditiveValue");
            SerializedProperty postMultiplyAdditiveValueProp = property.FindPropertyRelative("postMultiplyAdditiveValue");
            SerializedProperty setByCallerTagProp = property.FindPropertyRelative("setByCallerTag");
            SerializedProperty customCalculationProp = property.FindPropertyRelative("customCalculation");

            // Target section
            currentRect.y += LineHeight + VerticalSpacing;
            EditorGUI.LabelField(currentRect, "Target", EditorStyles.boldLabel);

            currentRect.y += LineHeight + VerticalSpacing;
            float attributeHeight = EditorGUI.GetPropertyHeight(attributeProp, true);
            currentRect.height = attributeHeight;
            EditorGUI.PropertyField(currentRect, attributeProp, new GUIContent("Attribute to Modify"), true);

            currentRect.y += attributeHeight + VerticalSpacing;
            currentRect.height = LineHeight;
            EditorGUI.PropertyField(currentRect, operationProp, new GUIContent("Operation"));

            // Magnitude Calculation section
            currentRect.y += LineHeight + VerticalSpacing;
            EditorGUI.LabelField(currentRect, "Magnitude Calculation", EditorStyles.boldLabel);

            currentRect.y += LineHeight + VerticalSpacing;
            currentRect.height = EditorGUI.GetPropertyHeight(calculationTypeProp, true);
            EditorGUI.PropertyField(currentRect, calculationTypeProp, new GUIContent("Calculation Type"), true);

            var calculationType = (EModifierCalculationType)calculationTypeProp.enumValueIndex;
            // Draw fields based on calculation type
            switch (calculationType)
            {
                case EModifierCalculationType.ScalableFloat:
                    currentRect.y += currentRect.height + VerticalSpacing;
                    float scalableHeight = EditorGUI.GetPropertyHeight(scalableMagnitudeProp, true);
                    currentRect.height = scalableHeight;
                    EditorGUI.PropertyField(currentRect, scalableMagnitudeProp, new GUIContent("Magnitude"), true);
                    break;
                case EModifierCalculationType.AttributeBased:
                    currentRect.y += currentRect.height + VerticalSpacing;
                    float backingAttributeHeight = EditorGUI.GetPropertyHeight(backingAttributeProp, true);
                    currentRect.height = backingAttributeHeight;
                    EditorGUI.PropertyField(currentRect, backingAttributeProp, new GUIContent("Backing Attribute"), true);

                    currentRect.y += backingAttributeHeight + VerticalSpacing;
                    currentRect.height = LineHeight;
                    EditorGUI.PropertyField(currentRect, attributeSourceProp, new GUIContent("Attribute Source"));

                    currentRect.y += LineHeight + VerticalSpacing;
                    EditorGUI.PropertyField(currentRect, snapshotAttributeProp, new GUIContent("Snapshot"));

                    currentRect.y += LineHeight + VerticalSpacing;
                    EditorGUI.LabelField(currentRect, "(base + pre)  * coefficient + post", EditorStyles.boldLabel);

                    currentRect.y += LineHeight + VerticalSpacing;
                    EditorGUI.PropertyField(currentRect, coefficientProp, new GUIContent("Coefficient"));

                    currentRect.y += LineHeight + VerticalSpacing;
                    EditorGUI.PropertyField(currentRect, preMultiplyAdditiveValueProp, new GUIContent("Pre-Multiply Add"));

                    currentRect.y += LineHeight + VerticalSpacing;
                    EditorGUI.PropertyField(currentRect, postMultiplyAdditiveValueProp, new GUIContent("Post-Multiply Add"));
                    break;

                case EModifierCalculationType.CustomCalculationClass:
                    currentRect.y += currentRect.height + VerticalSpacing;
                    float customCalculationHeight = EditorGUI.GetPropertyHeight(customCalculationProp, true);
                    currentRect.height = customCalculationHeight;
                    EditorGUI.PropertyField(currentRect, customCalculationProp, new GUIContent("Custom Calculation"), true);

                    currentRect.y += customCalculationHeight + VerticalSpacing;
                    float scalableMagnitudeHeightCC = EditorGUI.GetPropertyHeight(scalableMagnitudeProp, true);
                    currentRect.height = scalableMagnitudeHeightCC;
                    EditorGUI.PropertyField(currentRect, scalableMagnitudeProp, new GUIContent("Base Magnitude"), true);

                    currentRect.y += currentRect.height + VerticalSpacing;
                    currentRect.height = LineHeight * 2.5f;

                    bool hasCalculation = customCalculationProp.objectReferenceValue != null;
                    bool baseMagnitudeIsZero = scalableMagnitudeProp.FindPropertyRelative("baseValue") != null
                        && scalableMagnitudeProp.FindPropertyRelative("baseValue").floatValue == 0f;
                    string infoText;
                    MessageType infoType;
                    if (!hasCalculation)
                    {
                        infoText = "Assign a DamageCalculation asset (e.g., WC3DamageCalculation).";
                        infoType = MessageType.Warning;
                    }
                    else if (baseMagnitudeIsZero)
                    {
                        infoText = "Base Magnitude is 0-class reads attributes itself.";
                        infoType = MessageType.Warning;
                    }
                    else
                    {
                        infoText = "Selected calculation asset will determine the final magnitude.";
                        infoType = MessageType.Info;
                    }
                    EditorGUI.HelpBox(currentRect, infoText, infoType);
                    break;
                case EModifierCalculationType.SetByCaller:
                    currentRect.y += currentRect.height + VerticalSpacing;
                    float setByCallerHeight = EditorGUI.GetPropertyHeight(setByCallerTagProp, true);
                    currentRect.height = setByCallerHeight;
                    EditorGUI.PropertyField(currentRect, setByCallerTagProp, new GUIContent("Gameplay Tag"));
                    break;
            }

            EditorGUI.indentLevel = originalIndent;
            EditorGUI.EndProperty();
        }
    }
}
