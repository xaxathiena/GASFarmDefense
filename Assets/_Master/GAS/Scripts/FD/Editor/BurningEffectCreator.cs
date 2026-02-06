using UnityEngine;
using UnityEditor;
using GAS;

namespace FD.Editor
{
    /// <summary>
    /// Editor utility to create Burning Damage effect for FireAreaAbility
    /// </summary>
    public static class BurningEffectCreator
    {
        [MenuItem("FD/Create/Burning Damage Effect")]
        public static void CreateBurningDamageEffect()
        {
            // Create the GameplayEffect
            var effect = ScriptableObject.CreateInstance<GameplayEffect>();
            effect.effectName = "Burning Damage";
            effect.description = "Burns the target for 20 damage per second";
            
            // Set duration and periodic properties
            effect.durationType = EGameplayEffectDurationType.Infinite; // Infinite until enemy leaves fire area
            effect.isPeriodic = true;
            effect.period = 1f; // Every 1 second
            
            // Create modifier for damage
            effect.modifiers = new GameplayEffectModifier[]
            {
                new GameplayEffectModifier
                {
                    attribute = new AttributeSelector(EGameplayAttributeType.Health),
                    operation = EGameplayModifierOp.Add,
                    calculationType = EModifierCalculationType.ScalableFloat,
                    scalableMagnitude = new ScalableFloat(-20f) // -20 damage per second
                }
            };
            
            // Grant burning tag
            effect.grantedTags = new GameplayTag[] { GameplayTag.State_Burning };
            
            // Save the asset
            string path = "Assets/Prefabs/Abilities/GE_BurningDamage.asset";
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            AssetDatabase.CreateAsset(effect, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the asset
            Selection.activeObject = effect;
            EditorGUIUtility.PingObject(effect);
            
            Debug.Log($"Created Burning Damage GameplayEffect at: {path}");
        }
    }
}
