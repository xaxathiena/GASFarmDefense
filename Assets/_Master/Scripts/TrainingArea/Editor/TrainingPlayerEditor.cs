using UnityEngine;
using UnityEditor;
using FD.TrainingArea;
using GAS;

namespace FD.TrainingArea.Editor
{
    [CustomEditor(typeof(TrainingPlayer))]
    public class TrainingPlayerEditor : UnityEditor.Editor
    {
        private SerializedProperty abilitySystemComponentProp;
        private SerializedProperty initialEffectProp;
        private SerializedProperty availableAbilitiesProp;
        private SerializedProperty selectedAbilityIndexProp;
        private SerializedProperty targetTransformProp;
        private SerializedProperty autoTargetRangeProp;
        private SerializedProperty targetLayerProp;

        private void OnEnable()
        {
            abilitySystemComponentProp = serializedObject.FindProperty("abilitySystemComponent");
            initialEffectProp = serializedObject.FindProperty("initialEffect");
            availableAbilitiesProp = serializedObject.FindProperty("availableAbilities");
            selectedAbilityIndexProp = serializedObject.FindProperty("selectedAbilityIndex");
            targetTransformProp = serializedObject.FindProperty("targetTransform");
            autoTargetRangeProp = serializedObject.FindProperty("autoTargetRange");
            targetLayerProp = serializedObject.FindProperty("targetLayer");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            TrainingPlayer player = (TrainingPlayer)target;

            EditorGUILayout.LabelField("Training Player Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Base components
            EditorGUILayout.PropertyField(abilitySystemComponentProp);
            EditorGUILayout.PropertyField(initialEffectProp);
            EditorGUILayout.Space();

            // Ability Selection
            EditorGUILayout.LabelField("Ability Testing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(availableAbilitiesProp, true);
            EditorGUILayout.Space();

            // Ability dropdown selection
            if (availableAbilitiesProp.arraySize > 0)
            {
                string[] abilityNames = new string[availableAbilitiesProp.arraySize];
                for (int i = 0; i < availableAbilitiesProp.arraySize; i++)
                {
                    var ability = availableAbilitiesProp.GetArrayElementAtIndex(i).objectReferenceValue as GameplayAbility;
                    abilityNames[i] = ability != null ? ability.name : "None";
                }

                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup("Selected Ability", selectedAbilityIndexProp.intValue, abilityNames);
                if (EditorGUI.EndChangeCheck())
                {
                    selectedAbilityIndexProp.intValue = newIndex;
                }

                EditorGUILayout.Space();

                // Show selected ability details
                if (selectedAbilityIndexProp.intValue >= 0 && selectedAbilityIndexProp.intValue < availableAbilitiesProp.arraySize)
                {
                    var selectedAbility = availableAbilitiesProp.GetArrayElementAtIndex(selectedAbilityIndexProp.intValue).objectReferenceValue as GameplayAbility;
                    if (selectedAbility != null)
                    {
                        EditorGUILayout.LabelField("Selected Ability Details", EditorStyles.boldLabel);
                        
                        EditorGUI.BeginDisabledGroup(true);
                        UnityEditor.Editor abilityEditor = CreateEditor(selectedAbility);
                        abilityEditor.OnInspectorGUI();
                        EditorGUI.EndDisabledGroup();
                        
                        EditorGUILayout.Space();
                    }
                }

                // Test ability button
                if (Application.isPlaying)
                {
                    if (GUILayout.Button("Activate Selected Ability", GUILayout.Height(30)))
                    {
                        player.ActivateSelectedAbility();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Add abilities to the Available Abilities list to test them.", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Target Settings
            EditorGUILayout.LabelField("Target Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(targetTransformProp);
            EditorGUILayout.PropertyField(autoTargetRangeProp);
            EditorGUILayout.PropertyField(targetLayerProp);

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Find Nearest Target"))
                {
                    player.FindNearestTarget();
                }
                if (GUILayout.Button("Reset Stats"))
                {
                    player.ResetStats();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
