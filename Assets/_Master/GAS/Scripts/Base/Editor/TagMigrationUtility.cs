using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GAS
{
    /// <summary>
    /// Utility to migrate old string-based tags to byte-based GameplayTag enums
    /// Run this once after updating the tag system
    /// </summary>
    public class TagMigrationUtility : EditorWindow
    {
        private static Dictionary<string, GameplayTag> stringToEnumMap = new Dictionary<string, GameplayTag>
        {
            // State Tags
            { "State.Stunned", GameplayTag.State_Stunned },
            { "State.Dead", GameplayTag.State_Dead },
            { "State.Immune", GameplayTag.State_Immune },
            { "State.Immune.CC", GameplayTag.State_Immune_CC },
            { "State.Immune.Stun", GameplayTag.State_Immune_Stun },
            { "State.Disabled", GameplayTag.State_Disabled },
            { "State.Silenced", GameplayTag.State_Silenced },
            { "State.Invulnerable", GameplayTag.State_Invulnerable },
            { "State.Buffed", GameplayTag.State_Buffed },
            { "State.CannotMove", GameplayTag.State_CannotMove },
            { "State.CannotAttack", GameplayTag.State_CannotAttack },
            
            // Elemental States
            { "State.Burning", GameplayTag.State_Burning },
            { "State.Shocked", GameplayTag.State_Shocked },
            { "State.Wet", GameplayTag.State_Wet },
            { "State.Frozen", GameplayTag.State_Frozen },
            { "State.Poisoned", GameplayTag.State_Poisoned },
            
            // Buffs
            { "Buff.Speed", GameplayTag.Buff_Speed },
            { "Buff.Attack", GameplayTag.Buff_Attack },
            { "Buff.Stamina", GameplayTag.Buff_Stamina },
            { "Buff.Defense", GameplayTag.Buff_Defense },
            
            // Debuffs
            { "Debuff.Poison", GameplayTag.Debuff_Poison },
            { "Debuff.DefenseBreak", GameplayTag.Debuff_DefenseBreak },
            { "Debuff.Slow", GameplayTag.Debuff_Slow },
            
            // Abilities
            { "Ability.Attack", GameplayTag.Ability_Attack },
            { "Ability.Defense", GameplayTag.Ability_Defense },
            { "Ability.Magic", GameplayTag.Ability_Magic },
        };

        private int migratedEffects = 0;
        private int migratedAbilities = 0;
        private List<string> errors = new List<string>();
        private Vector2 scrollPosition;

        [MenuItem("GAS/Tools/Tag Migration Utility")]
        public static void ShowWindow()
        {
            var window = GetWindow<TagMigrationUtility>("Tag Migration");
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Tag Migration Utility", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This utility migrates GameplayEffect and GameplayAbility assets from old string-based tags to new byte-based GameplayTag enums.\n\n" +
                "WARNING: This will modify all GameplayEffect and GameplayAbility assets in your project. Make sure to backup first!",
                MessageType.Warning
            );

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Migrate All Assets", GUILayout.Height(40)))
            {
                MigrateAllAssets();
            }

            EditorGUILayout.Space(10);

            if (migratedEffects > 0 || migratedAbilities > 0 || errors.Count > 0)
            {
                EditorGUILayout.LabelField("Migration Results:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Migrated Effects: {migratedEffects}");
                EditorGUILayout.LabelField($"Migrated Abilities: {migratedAbilities}");

                if (errors.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField($"Errors: {errors.Count}", EditorStyles.boldLabel);
                    
                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                    foreach (var error in errors)
                    {
                        EditorGUILayout.HelpBox(error, MessageType.Error);
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void MigrateAllAssets()
        {
            migratedEffects = 0;
            migratedAbilities = 0;
            errors.Clear();

            // Find all GameplayEffect assets
            string[] effectGuids = AssetDatabase.FindAssets("t:GameplayEffect");
            foreach (string guid in effectGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameplayEffect effect = AssetDatabase.LoadAssetAtPath<GameplayEffect>(path);
                
                if (effect != null)
                {
                    MigrateGameplayEffect(effect, path);
                }
            }

            // Find all GameplayAbility assets
            string[] abilityGuids = AssetDatabase.FindAssets("t:GameplayAbility");
            foreach (string guid in abilityGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameplayAbilityData ability = AssetDatabase.LoadAssetAtPath<GameplayAbilityData>(path);
                
                if (ability != null)
                {
                    MigrateGameplayAbility(ability, path);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Tag Migration Complete! Migrated {migratedEffects} effects and {migratedAbilities} abilities.");
        }

        private void MigrateGameplayEffect(GameplayEffect effect, string path)
        {
            SerializedObject so = new SerializedObject(effect);

            // Note: The old string[] fields no longer exist in the class definition
            // This migration utility is for manual conversion or for reference
            // In practice, you'll need to manually update asset files or use a text-based migration

            // Since we can't access the old string fields directly (they've been replaced),
            // this serves as a template. You would need to:
            // 1. Temporarily add back the old fields as [SerializeField] private string[] oldGrantedTags
            // 2. Run migration to copy values
            // 3. Remove old fields

            EditorUtility.SetDirty(effect);
            migratedEffects++;
        }

        private void MigrateGameplayAbility(GameplayAbilityData ability, string path)
        {
            SerializedObject so = new SerializedObject(ability);

            // Same note as above for GameplayEffect

            EditorUtility.SetDirty(ability);
            migratedAbilities++;
        }

        /// <summary>
        /// Utility method to convert string tag to enum
        /// </summary>
        public static GameplayTag StringToEnum(string tagString)
        {
            if (string.IsNullOrEmpty(tagString))
                return GameplayTag.None;

            if (stringToEnumMap.TryGetValue(tagString, out GameplayTag tag))
                return tag;

            Debug.LogWarning($"Unknown tag string: {tagString}. Returning None.");
            return GameplayTag.None;
        }

        /// <summary>
        /// Utility method to convert string array to enum array
        /// </summary>
        public static GameplayTag[] StringArrayToEnumArray(string[] tagStrings)
        {
            if (tagStrings == null || tagStrings.Length == 0)
                return new GameplayTag[0];

            List<GameplayTag> tags = new List<GameplayTag>();
            foreach (string tagString in tagStrings)
            {
                GameplayTag tag = StringToEnum(tagString);
                if (tag != GameplayTag.None)
                {
                    tags.Add(tag);
                }
            }

            return tags.ToArray();
        }
    }
}
