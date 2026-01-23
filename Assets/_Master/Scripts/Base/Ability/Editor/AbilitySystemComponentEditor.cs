using UnityEditor;
using UnityEngine;
using _Master.Base.Ability;
using System.Reflection;

namespace _Master.Base.Ability.Editor
{
    [CustomEditor(typeof(AbilitySystemComponent))]
    public class AbilitySystemComponentEditor : UnityEditor.Editor
    {
        private AbilitySystemComponent asc;
        private bool showAttributes = true;
        private bool showAbilities = true;
        private bool showEffects = true;
        private bool showTags = true;

        private void OnEnable()
        {
            asc = (AbilitySystemComponent)target;
        }

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Attribute values will be shown during Play Mode", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Draw Attributes
            DrawAttributeSet();

            EditorGUILayout.Space(5);

            // Draw Resources
            DrawResources();

            EditorGUILayout.Space(5);

            // Draw Abilities
            DrawAbilities();

            EditorGUILayout.Space(5);

            // Draw Active Effects
            DrawActiveEffects();

            EditorGUILayout.Space(5);

            // Draw Tags
            DrawTags();

            // Force repaint to update values in real-time
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawAttributeSet()
        {
            showAttributes = EditorGUILayout.Foldout(showAttributes, "Attribute Set", true, EditorStyles.foldoutHeader);
            if (!showAttributes) return;

            EditorGUI.indentLevel++;

            var attributeSet = asc.AttributeSet;
            if (attributeSet == null)
            {
                EditorGUILayout.HelpBox("No AttributeSet initialized", MessageType.Warning);
                EditorGUI.indentLevel--;
                return;
            }

            // Get all public properties that are GameplayAttribute
            var properties = attributeSet.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(GameplayAttribute))
                {
                    var attribute = prop.GetValue(attributeSet) as GameplayAttribute;
                    if (attribute != null)
                    {
                        DrawGameplayAttribute(prop.Name, attribute);
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawGameplayAttribute(string name, GameplayAttribute attribute)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            
            // Base Value
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Base Value", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            
            // Current Value with color
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current Value", GUILayout.Width(100));
            
            
            EditorGUILayout.LabelField(attribute.CurrentValue.ToString("F2"));
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel--;
            
            EditorGUILayout.EndVertical();
        }

        private void DrawResources()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Resources", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            
            // Current Resource
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current", GUILayout.Width(100));
            EditorGUILayout.LabelField($"{asc.CurrentMana:F2} / {asc.MaxMana:F2}");
            EditorGUILayout.EndHorizontal();
            
            // Progress Bar
            Rect rect = EditorGUILayout.GetControlRect(false, 20);
            float percentage = asc.CurrentMana / asc.MaxMana;
            EditorGUI.ProgressBar(rect, percentage, $"{percentage * 100:F0}%");
            
            EditorGUI.indentLevel--;
            
            EditorGUILayout.EndVertical();
        }

        private void DrawAbilities()
        {
            showAbilities = EditorGUILayout.Foldout(showAbilities, "Abilities", true, EditorStyles.foldoutHeader);
            if (!showAbilities) return;

            EditorGUI.indentLevel++;

            var grantedAbilitiesField = typeof(AbilitySystemComponent).GetField("grantedAbilities", BindingFlags.NonPublic | BindingFlags.Instance);
            var grantedAbilities = grantedAbilitiesField?.GetValue(asc) as System.Collections.Generic.List<GameplayAbility>;

            if (grantedAbilities == null || grantedAbilities.Count == 0)
            {
                EditorGUILayout.LabelField("No abilities granted", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var ability in grantedAbilities)
                {
                    if (ability != null)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        
                        EditorGUILayout.LabelField(ability.abilityName, EditorStyles.boldLabel);
                        
                        EditorGUI.indentLevel++;
                        
                        // Cooldown info
                        if (asc.IsAbilityOnCooldown(ability))
                        {
                            float remaining = asc.GetAbilityCooldownRemaining(ability);
                            EditorGUILayout.LabelField($"Cooldown: {remaining:F1}s", EditorStyles.miniLabel);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Ready", EditorStyles.miniLabel);
                        }
                        
                        EditorGUI.indentLevel--;
                        
                        EditorGUILayout.EndVertical();
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawActiveEffects()
        {
            showEffects = EditorGUILayout.Foldout(showEffects, "Active Gameplay Effects", true, EditorStyles.foldoutHeader);
            if (!showEffects) return;

            EditorGUI.indentLevel++;

            var activeEffects = asc.GetActiveGameplayEffects();

            if (activeEffects == null || activeEffects.Count == 0)
            {
                EditorGUILayout.LabelField("No active effects", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var activeEffect in activeEffects)
                {
                    if (activeEffect != null && activeEffect.Effect != null)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        
                        EditorGUILayout.LabelField(activeEffect.Effect.effectName, EditorStyles.boldLabel);
                        
                        EditorGUI.indentLevel++;
                        
                        // Duration
                        if (activeEffect.Duration > 0)
                        {
                            EditorGUILayout.LabelField($"Time Remaining: {activeEffect.RemainingTime:F1}s / {activeEffect.Duration:F1}s");
                            
                            Rect rect = EditorGUILayout.GetControlRect(false, 15);
                            float percentage = activeEffect.RemainingTime / activeEffect.Duration;
                            EditorGUI.ProgressBar(rect, percentage, "");
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Duration: Infinite");
                        }
                        
                        // Stack count
                        if (activeEffect.StackCount > 1)
                        {
                            EditorGUILayout.LabelField($"Stacks: {activeEffect.StackCount}");
                        }
                        
                        EditorGUI.indentLevel--;
                        
                        EditorGUILayout.EndVertical();
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawTags()
        {
            showTags = EditorGUILayout.Foldout(showTags, "Active Tags", true, EditorStyles.foldoutHeader);
            if (!showTags) return;

            EditorGUI.indentLevel++;

            var activeTagsField = typeof(AbilitySystemComponent).GetField("activeTags", BindingFlags.NonPublic | BindingFlags.Instance);
            var activeTags = activeTagsField?.GetValue(asc) as System.Collections.Generic.HashSet<string>;

            if (activeTags == null || activeTags.Count == 0)
            {
                EditorGUILayout.LabelField("No active tags", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var tag in activeTags)
                {
                    EditorGUILayout.LabelField($"• {tag}", EditorStyles.miniLabel);
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}
