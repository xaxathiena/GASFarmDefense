using UnityEditor;
using UnityEngine;
using GAS;
using System.Collections.Generic;

namespace FD.Editor
{
    /// <summary>
    /// Editor Window for debugging Ability System Component runtime info.
    /// Access via: Window > GAS > ASC Debug Inspector
    /// </summary>
    public class GASDebugWindow : EditorWindow
    {
        [MenuItem("Window/GAS/ASC Debug Inspector")]
        public static void ShowWindow()
        {
            var window = GetWindow<GASDebugWindow>("ASC Debug");
            window.minSize = new Vector2(400, 600);
        }
        
        private Vector2 scrollPosition;
        private int selectedObjectIndex = 0;
        private string[] objectNames = new string[0];
        private List<object> trackedObjects = new List<object>();
        
        private AbilitySystemComponent selectedASC;
        
        // Foldouts
        private bool showAttributes = true;
        private bool showAbilities = true;
        private bool showCooldowns = true;
        private bool showTags = true;
        private bool showEffects = true;
        
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
        
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
        
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                // Clear when exiting play mode
                selectedASC = null;
                trackedObjects.Clear();
                objectNames = new string[0];
                selectedObjectIndex = 0;
            }
        }
        
        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("ASC Debug Inspector only works in Play Mode", MessageType.Info);
                return;
            }
            
            // Refresh objects list
            RefreshObjectsList();
            
            if (objectNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No objects with ASC found. Make sure FDBattleManager has spawned towers/enemies.", MessageType.Warning);
                return;
            }
            
            // Object selection dropdown
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Select Object:", GUILayout.Width(100));
            
            int newIndex = EditorGUILayout.Popup(selectedObjectIndex, objectNames, EditorStyles.toolbarPopup);
            if (newIndex != selectedObjectIndex)
            {
                selectedObjectIndex = newIndex;
                UpdateSelectedASC();
            }
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshObjectsList();
                UpdateSelectedASC();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (selectedASC == null)
            {
                EditorGUILayout.HelpBox("No ASC selected", MessageType.Warning);
                return;
            }
            
            // Auto-refresh toggle
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Auto-refresh:", GUILayout.Width(100));
            bool autoRefresh = EditorGUILayout.Toggle(EditorPrefs.GetBool("GASDebug_AutoRefresh", true));
            EditorPrefs.SetBool("GASDebug_AutoRefresh", autoRefresh);
            if (autoRefresh)
            {
                Repaint(); // Continuous repaint for real-time updates
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Scrollable content area
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Draw ASC info sections
            DrawAttributeSet();
            DrawAbilities();
            DrawCooldowns();
            DrawTags();
            DrawActiveEffects();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void RefreshObjectsList()
        {
            trackedObjects.Clear();
            var namesList = new List<string>();
            
            var battleManager = FDBattleManager.Instance;
            if (battleManager == null)
            {
                objectNames = new string[0];
                return;
            }
            
            // Add towers
            var towers = battleManager.GetAllTowers();
            foreach (var tower in towers)
            {
                trackedObjects.Add(tower);
                namesList.Add($"🗼 {tower.DisplayName}");
            }
            
            // Add enemies
            var enemies = battleManager.GetAllEnemies();
            foreach (var enemy in enemies)
            {
                trackedObjects.Add(enemy);
                namesList.Add($"👾 {enemy.DisplayName}");
            }
            
            objectNames = namesList.ToArray();
            
            // Ensure valid selection
            if (selectedObjectIndex >= objectNames.Length)
            {
                selectedObjectIndex = 0;
            }
        }
        
        private void UpdateSelectedASC()
        {
            if (selectedObjectIndex < 0 || selectedObjectIndex >= trackedObjects.Count)
            {
                selectedASC = null;
                return;
            }
            
            var obj = trackedObjects[selectedObjectIndex];
            
            if (obj is TowerController tower)
            {
                selectedASC = tower.AbilitySystemComponent;
            }
            else if (obj is EnemyController enemy)
            {
                selectedASC = enemy.AbilitySystemComponent;
            }
        }
        
        private void DrawAttributeSet()
        {
            showAttributes = EditorGUILayout.Foldout(showAttributes, "Attributes", true, EditorStyles.foldoutHeader);
            if (!showAttributes) return;
            
            EditorGUI.indentLevel++;
            
            var attributeSet = selectedASC.AttributeSet;
            if (attributeSet == null)
            {
                EditorGUILayout.HelpBox("No AttributeSet initialized", MessageType.Warning);
                EditorGUI.indentLevel--;
                return;
            }
            
            // Get all attributes (both fields and properties)
            var type = attributeSet.GetType();
            
            // Get public fields
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameplayAttribute))
                {
                    var attribute = field.GetValue(attributeSet) as GameplayAttribute;
                    if (attribute != null)
                    {
                        DrawGameplayAttribute(field.Name, attribute);
                    }
                }
            }
            
            // Get public properties
            var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
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
            EditorGUILayout.Space(5);
        }
        
        private void DrawGameplayAttribute(string name, GameplayAttribute attribute)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Base Value:", GUILayout.Width(100));
            EditorGUILayout.LabelField(attribute.BaseValue.ToString("F2"));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current Value:", GUILayout.Width(100));
            
            // Color based on value
            Color originalColor = GUI.contentColor;
            if (attribute.CurrentValue < attribute.BaseValue)
                GUI.contentColor = Color.red;
            else if (attribute.CurrentValue > attribute.BaseValue)
                GUI.contentColor = Color.green;
            
            EditorGUILayout.LabelField(attribute.CurrentValue.ToString("F2"));
            GUI.contentColor = originalColor;
            EditorGUILayout.EndHorizontal();
            
            // Show modifiers if any
            var modifiers = attribute.GetActiveModifiers();
            if (modifiers != null && modifiers.Count > 0)
            {
                EditorGUILayout.LabelField($"Modifiers: {modifiers.Count}", EditorStyles.miniLabel);
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAbilities()
        {
            showAbilities = EditorGUILayout.Foldout(showAbilities, "Granted Abilities", true, EditorStyles.foldoutHeader);
            if (!showAbilities) return;
            
            EditorGUI.indentLevel++;
            
            var grantedAbilities = selectedASC.EditorGetGrantedAbilities();
            
            if (grantedAbilities.Count == 0)
            {
                EditorGUILayout.HelpBox("No abilities granted", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }
            
            foreach (var ability in grantedAbilities)
            {
                if (ability == null) continue;
                
                var spec = selectedASC.GetAbilitySpec(ability);
                bool isActive = spec != null && spec.IsActive;
                bool isOnCooldown = selectedASC.IsAbilityOnCooldown(ability);
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Ability name with status indicator
                EditorGUILayout.BeginHorizontal();
                
                // Status dot
                Color statusColor = isActive ? Color.green : (isOnCooldown ? Color.yellow : Color.gray);
                GUI.color = statusColor;
                EditorGUILayout.LabelField("●", GUILayout.Width(20));
                GUI.color = Color.white;
                
                // Show ability name (or type name if empty)
                string displayName = string.IsNullOrEmpty(ability.abilityName) 
                    ? $"[{ability.GetType().Name}]" 
                    : ability.abilityName;
                EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel++;
                
                if (spec != null)
                {
                    EditorGUILayout.LabelField($"Level: {spec.Level}");
                    EditorGUILayout.LabelField($"Active: {isActive}");
                    
                    // Warning if stuck in active state
                    if (isActive && !isOnCooldown)
                    {
                        EditorGUILayout.HelpBox("⚠️ Ability stuck in Active state! May not have called EndAbility().", MessageType.Warning);
                    }
                    
                    // Show cost if any
                    float costValue = ability.costAmount.GetValueAtLevel(spec.Level, selectedASC);
                    if (costValue > 0)
                    {
                        EditorGUILayout.LabelField($"Cost: {costValue:F0} Mana");
                    }
                }
                
                if (isOnCooldown)
                {
                    float remaining = selectedASC.GetAbilityCooldownRemaining(ability);
                    float maxCooldown = ability.cooldownDuration.GetValueAtLevel(1f, selectedASC);
                    float progress = maxCooldown > 0 ? 1f - (remaining / maxCooldown) : 1f;
                    
                    Rect rect = EditorGUILayout.GetControlRect(false, 20);
                    EditorGUI.ProgressBar(rect, progress, $"Cooldown: {remaining:F1}s");
                }
                
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }
        
        private void DrawCooldowns()
        {
            showCooldowns = EditorGUILayout.Foldout(showCooldowns, "Active Cooldowns", true, EditorStyles.foldoutHeader);
            if (!showCooldowns) return;
            
            EditorGUI.indentLevel++;
            
            var abilityCooldowns = selectedASC.EditorGetAbilityCooldowns();
            
            if (abilityCooldowns.Count == 0)
            {
                EditorGUILayout.HelpBox("No active cooldowns", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }
            
            foreach (var kvp in abilityCooldowns)
            {
                if (kvp.Key == null) continue;
                
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                // Show ability name (or type name if empty)
                string displayName = string.IsNullOrEmpty(kvp.Key.abilityName) 
                    ? $"[{kvp.Key.GetType().Name}]" 
                    : kvp.Key.abilityName;
                EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel, GUILayout.Width(180));
                
                float maxCooldown = kvp.Key.cooldownDuration.GetValueAtLevel(1f, selectedASC);
                float progress = maxCooldown > 0 ? 1f - (kvp.Value / maxCooldown) : 1f;
                
                Rect rect = EditorGUILayout.GetControlRect(false, 18);
                EditorGUI.ProgressBar(rect, progress, $"{kvp.Value:F1}s");
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }
        
        private void DrawTags()
        {
            showTags = EditorGUILayout.Foldout(showTags, "Active Tags", true, EditorStyles.foldoutHeader);
            if (!showTags) return;
            
            EditorGUI.indentLevel++;
            
            var activeTags = selectedASC.GetActiveTags();
            
            if (activeTags.Count == 0)
            {
                EditorGUILayout.HelpBox("No active tags", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }
            
            foreach (var tag in activeTags)
            {
                int count = selectedASC.GetTagCount(tag);
                
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"● {tag}", GUILayout.Width(200));
                
                if (count > 1)
                {
                    EditorGUILayout.LabelField($"x{count}", EditorStyles.miniLabel, GUILayout.Width(40));
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }
        
        private void DrawActiveEffects()
        {
            showEffects = EditorGUILayout.Foldout(showEffects, "Active Gameplay Effects", true, EditorStyles.foldoutHeader);
            if (!showEffects) return;
            
            EditorGUI.indentLevel++;
            
            var activeEffects = selectedASC.EditorGetActiveEffects();
            
            if (activeEffects.Count == 0)
            {
                EditorGUILayout.HelpBox("No active effects", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }
            
            foreach (var activeEffect in activeEffects)
            {
                if (activeEffect == null || activeEffect.Effect == null) continue;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField(activeEffect.Effect.effectName, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField($"Level: {activeEffect.Level}");
                EditorGUILayout.LabelField($"Stack Count: {activeEffect.StackCount}");
                
                if (activeEffect.Effect.durationType == EGameplayEffectDurationType.Duration)
                {
                    float remaining = activeEffect.RemainingTime;
                    EditorGUILayout.LabelField($"Duration: {remaining:F1}s");
                }
                
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }
    }
}
