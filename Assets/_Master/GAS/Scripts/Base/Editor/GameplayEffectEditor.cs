using UnityEditor;
using UnityEngine;

namespace GAS.Editor
{
    /// <summary>
    /// Custom editor for GameplayEffect with improved layout and helpers
    /// </summary>
    [CustomEditor(typeof(GameplayEffect))]
    public class GameplayEffectEditor : UnityEditor.Editor
    {
        private SerializedProperty effectNameProp;
        private SerializedProperty descriptionProp;
        private SerializedProperty durationTypeProp;
        private SerializedProperty durationMagnitudeProp;
        private SerializedProperty isPeriodicProp;
        private SerializedProperty periodProp;
        private SerializedProperty modifiersProp;
        private SerializedProperty grantedTagsProp;
        private SerializedProperty applicationRequiredTagsProp;
        private SerializedProperty applicationBlockedByTagsProp;
        private SerializedProperty removeTagsOnApplicationProp;
        private SerializedProperty allowStackingProp;
        private SerializedProperty maxStacksProp;
        private SerializedProperty refreshDurationOnStackProp;

        private bool showDurationSettings = true;
        private bool showModifiers = true;
        private bool showTags = true;
        private bool showStacking = true;

        private void OnEnable()
        {
            effectNameProp = serializedObject.FindProperty("effectName");
            descriptionProp = serializedObject.FindProperty("description");
            durationTypeProp = serializedObject.FindProperty("durationType");
            durationMagnitudeProp = serializedObject.FindProperty("durationMagnitude");
            isPeriodicProp = serializedObject.FindProperty("isPeriodic");
            periodProp = serializedObject.FindProperty("period");
            modifiersProp = serializedObject.FindProperty("modifiers");
            grantedTagsProp = serializedObject.FindProperty("grantedTags");
            applicationRequiredTagsProp = serializedObject.FindProperty("applicationRequiredTags");
            applicationBlockedByTagsProp = serializedObject.FindProperty("applicationBlockedByTags");
            removeTagsOnApplicationProp = serializedObject.FindProperty("removeTagsOnApplication");
            allowStackingProp = serializedObject.FindProperty("allowStacking");
            maxStacksProp = serializedObject.FindProperty("maxStacks");
            refreshDurationOnStackProp = serializedObject.FindProperty("refreshDurationOnStack");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawEffectHeader();
            EditorGUILayout.Space(5);

            // Effect Info
            DrawEffectInfo();
            EditorGUILayout.Space(10);

            // Duration Settings
            showDurationSettings = EditorGUILayout.Foldout(showDurationSettings, "Duration & Timing", true);
            if (showDurationSettings)
            {
                DrawDurationSettings();
            }
            EditorGUILayout.Space(10);

            // Modifiers
            showModifiers = EditorGUILayout.Foldout(showModifiers, "Modifiers", true);
            if (showModifiers)
            {
                DrawModifiers();
            }
            EditorGUILayout.Space(10);

            // Gameplay Tags
            showTags = EditorGUILayout.Foldout(showTags, "Gameplay Tags", true);
            if (showTags)
            {
                DrawGameplayTags();
            }
            EditorGUILayout.Space(10);

            // Stacking
            showStacking = EditorGUILayout.Foldout(showStacking, "Stacking", true);
            if (showStacking)
            {
                DrawStacking();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEffectHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("Gameplay Effect", titleStyle);
            EditorGUILayout.LabelField("Based on Unreal Engine GAS", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawEffectInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Effect Info", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(effectNameProp);
            EditorGUILayout.PropertyField(descriptionProp);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawDurationSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.PropertyField(durationTypeProp);
            
            var durationType = (EGameplayEffectDurationType)durationTypeProp.enumValueIndex;
            
            // Show duration only for Duration type
            if (durationType == EGameplayEffectDurationType.Duration)
            {
                EditorGUILayout.PropertyField(durationMagnitudeProp, new GUIContent("Duration (seconds)"));
            }
            
            // Show periodic options for Duration and Infinite
            if (durationType != EGameplayEffectDurationType.Instant)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Periodic Execution", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(isPeriodicProp, new GUIContent("Is Periodic"));
                
                if (isPeriodicProp.boolValue)
                {
                    EditorGUILayout.PropertyField(periodProp, new GUIContent("Period (seconds)"));
                    
                    EditorGUILayout.HelpBox(
                        "Periodic effects execute modifiers every X seconds. " +
                        "Useful for damage/healing over time (DOT/HOT).",
                        MessageType.Info
                    );
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Instant effects apply once immediately to BaseValue. " +
                    "Tags will not be applied.",
                    MessageType.Info
                );
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawModifiers()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.HelpBox(
                "Modifiers change attributes. Each modifier can use:\n" +
                "• ScalableFloat: Fixed or curve-based values\n" +
                "• Attribute Based: Use source/target attributes\n" +
                "• SetByCaller: Runtime-defined values\n" +
                "• Custom Calculation: Advanced logic (future)",
                MessageType.Info
            );
            
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(modifiersProp, true);
            
            if (modifiersProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No modifiers defined. Add at least one to affect attributes.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawGameplayTags()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.PropertyField(grantedTagsProp, new GUIContent("Granted Tags"));
            EditorGUILayout.HelpBox("Tags added while effect is active", MessageType.None);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(applicationRequiredTagsProp, new GUIContent("Required Tags"));
            EditorGUILayout.HelpBox("Target must have ALL these tags to apply", MessageType.None);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(applicationBlockedByTagsProp, new GUIContent("Blocked By Tags"));
            EditorGUILayout.HelpBox("Effect blocked if target has ANY of these tags", MessageType.None);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(removeTagsOnApplicationProp, new GUIContent("Remove Tags"));
            EditorGUILayout.HelpBox("Tags removed when effect is applied", MessageType.None);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawStacking()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.PropertyField(allowStackingProp);
            
            if (allowStackingProp.boolValue)
            {
                EditorGUILayout.PropertyField(maxStacksProp);
                EditorGUILayout.PropertyField(refreshDurationOnStackProp, new GUIContent("Refresh Duration"));
                
                EditorGUILayout.HelpBox(
                    "Stacking multiplies modifier magnitudes by stack count.",
                    MessageType.Info
                );
            }
            
            EditorGUILayout.EndVertical();
        }
    }
}
