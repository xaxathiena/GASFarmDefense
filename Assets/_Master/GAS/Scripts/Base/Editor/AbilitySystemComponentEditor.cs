using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace GAS.Editor
{
    //[CustomEditor(typeof(AbilitySystemComponent))]
    public class AbilitySystemComponentEditor : UnityEditor.Editor
    {
        private AbilitySystemComponent asc;
        private bool showAttributes = true;
        private bool showAbilities = true;
        private bool showEffects = true;
        private bool showTags = true;

        private void OnEnable()
        {
            //asc = (AbilitySystemComponent)target;
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
            // Draw Tags
            DrawTags();


            EditorGUILayout.Space(5);
            // Draw Active Effects
            DrawActiveEffects();


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
            EditorGUILayout.LabelField(attribute.BaseValue.ToString("F2"));
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

            var attributeSet = asc.AttributeSet;
            var manaAttr = attributeSet?.GetAttribute(EGameplayAttributeType.Mana);
            var maxManaAttr = attributeSet?.GetAttribute(EGameplayAttributeType.MaxMana);

            float current = manaAttr != null ? manaAttr.CurrentValue : 0f;
            float max = maxManaAttr != null ? maxManaAttr.CurrentValue : 0f;

            // Current Resource
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current", GUILayout.Width(100));
            EditorGUILayout.LabelField($"{current:F2} / {max:F2}");
            EditorGUILayout.EndHorizontal();

            // Progress Bar
            Rect rect = EditorGUILayout.GetControlRect(false, 20);
            float percentage = max > 0f ? current / max : 0f;
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
            var grantedAbilities = grantedAbilitiesField?.GetValue(asc) as System.Collections.Generic.List<GameplayAbilityData>;

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
                EditorGUILayout.LabelField($"Total Active Effects: {activeEffects.Count}", EditorStyles.miniLabel);
                EditorGUILayout.Space(3);

                foreach (var activeEffect in activeEffects)
                {
                    if (activeEffect != null && activeEffect.Effect != null)
                    {
                        DrawActiveEffectDetails(activeEffect);
                        EditorGUILayout.Space(2);
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawActiveEffectDetails(ActiveGameplayEffect activeEffect)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header with effect name
            EditorGUILayout.LabelField(activeEffect.Effect.effectName, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            // Source and Target information
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Source:", GUILayout.Width(100));
            string sourceName = activeEffect.Source != null ? activeEffect.Source.GetOwner().name : "None";
            EditorGUILayout.LabelField(sourceName, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target:", GUILayout.Width(100));
            string targetName = activeEffect.Target != null ? activeEffect.Target.GetOwner().name : "None";
            EditorGUILayout.LabelField(targetName, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // Level
            if (activeEffect.Level > 1f)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Level:", GUILayout.Width(100));
                EditorGUILayout.LabelField(activeEffect.Level.ToString("F1"), EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            // Duration with progress bar
            if (activeEffect.Duration > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Duration:", GUILayout.Width(100));
                EditorGUILayout.LabelField($"{activeEffect.RemainingTime:F1}s / {activeEffect.Duration:F1}s", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                Rect rect = EditorGUILayout.GetControlRect(false, 12);
                float percentage = activeEffect.Duration > 0 ? activeEffect.RemainingTime / activeEffect.Duration : 0f;
                EditorGUI.ProgressBar(rect, percentage, "");
            }
            else if (activeEffect.Duration < 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Duration:", GUILayout.Width(100));
                EditorGUILayout.LabelField("Infinite", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Type:", GUILayout.Width(100));
                EditorGUILayout.LabelField("Instant", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            // Stack count
            if (activeEffect.Effect.allowStacking)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Stacks:", GUILayout.Width(100));
                EditorGUILayout.LabelField($"{activeEffect.StackCount} / {activeEffect.Effect.maxStacks}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            // Periodic info
            if (activeEffect.Effect.isPeriodic)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Periodic:", GUILayout.Width(100));
                EditorGUILayout.LabelField($"Every {activeEffect.Effect.period:F1}s", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            // Granted Tags
            if (activeEffect.Effect.grantedTags != null && activeEffect.Effect.grantedTags.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Tags:", GUILayout.Width(100));
                EditorGUILayout.BeginVertical();
                foreach (var tag in activeEffect.Effect.grantedTags)
                {
                    EditorGUILayout.LabelField($"• {tag}", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }

            // Affected Attributes
            var affectedAttributes = activeEffect.GetAffectedAttributes();
            if (affectedAttributes != null && affectedAttributes.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Modifies:", GUILayout.Width(100));
                EditorGUILayout.BeginVertical();
                foreach (var attr in affectedAttributes)
                {
                    if (attr != null)
                    {
                        // Try to find the attribute name
                        string attrName = GetAttributeName(attr);
                        EditorGUILayout.LabelField($"• {attrName}", EditorStyles.miniLabel);
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }

            // Modifiers summary
            if (activeEffect.Effect.modifiers != null && activeEffect.Effect.modifiers.Length > 0)
            {
                EditorGUILayout.LabelField($"Modifiers: {activeEffect.Effect.modifiers.Length}", EditorStyles.miniLabel);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }

        private string GetAttributeName(GameplayAttribute attribute)
        {
            if (attribute == null || asc.AttributeSet == null)
                return "Unknown";

            // Try to find which property this attribute belongs to
            var properties = asc.AttributeSet.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(GameplayAttribute))
                {
                    var propAttr = prop.GetValue(asc.AttributeSet) as GameplayAttribute;
                    if (propAttr == attribute)
                    {
                        return prop.Name;
                    }
                }
            }

            return "Unknown";
        }

        private void DrawTags()
        {
            showTags = EditorGUILayout.Foldout(showTags, "Active Tags", true, EditorStyles.foldoutHeader);
            if (!showTags) return;

            EditorGUI.indentLevel++;

            // Use the new GetActiveTags() method instead of reflection
            var activeTags = asc.GetActiveTags();

            if (activeTags == null || activeTags.Count == 0)
            {
                EditorGUILayout.LabelField("No active tags", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var tag in activeTags)
                {
                    int count = asc.GetTagCount(tag);
                    byte tagByte = (byte)tag;

                    // Show tag name and reference count
                    string countLabel = count > 1 ? $" [x{count}]" : "";
                    EditorGUILayout.LabelField($"• {tag} ({tagByte}){countLabel}", EditorStyles.miniLabel);
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}
