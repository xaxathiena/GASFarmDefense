#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GAS;
using Abel.TranHuongDao.Core.Abilities;

namespace Abel.TranHuongDao.Editor
{
    public static class AoEApplyEffectAssetCreator
    {
        private const string FOLDER_PATH = "Assets/_Master/TranHuongDao/Core/Abilities/Data";

        [MenuItem("GAS Farm Defense/Procs/Create All AoE Apply Effect Abilities")]
        public static void CreateAssets()
        {
            EnsureFolderExists();

            // 1. Giảm 12% Tốc Chạy (AoE Slow)
            CreateAoEDebuffAsset("AoESlow_12", EAuraTargetType.Enemies, EGameplayAttributeType.MoveSpeed, EGameplayModifierOp.Multiply, 0.88f, 800f);

            // 2. Giảm 20% Sát Thương (Attack Power)
            CreateAoEDebuffAsset("AoEDamageReduce_20", EAuraTargetType.Enemies, EGameplayAttributeType.AttackPower, EGameplayModifierOp.Multiply, 0.80f, 800f);

            // 3. Giảm 50% Giáp (Armor)
            CreateAoEDebuffAsset("AoEArmorReduce_50", EAuraTargetType.Enemies, EGameplayAttributeType.Armor, EGameplayModifierOp.Multiply, 0.50f, 800f);

            // 4. Giảm 30% Tốc Đánh (Attack Cooldown -> Multiply by 1.3 to increase delay)
            CreateAoEDebuffAsset("AoEBaseDamageReduce_30", EAuraTargetType.Enemies, EGameplayAttributeType.BaseDamage, EGameplayModifierOp.Multiply, 0.70f, 800f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Successfully generated all AoE Apply Effect abilities in the Data folder!");
        }

        public static void CreateAoEDebuffAsset(string name, EAuraTargetType targetType, EGameplayAttributeType attribute, EGameplayModifierOp modifierOp, float magnitude, float captureRadius)
        {
            // 1. Create the Gameplay Effect
            string effectPath = $"{FOLDER_PATH}/GE_{name}.asset";
            var effect = AssetDatabase.LoadAssetAtPath<GameplayEffect>(effectPath);
            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<GameplayEffect>();
                
                effect.durationType = EGameplayEffectDurationType.Infinite;
                effect.durationMagnitude = 0f;

                var modifier = new GameplayEffectModifier(attribute, modifierOp, magnitude);
                effect.modifiers = new[] { modifier };

                AssetDatabase.CreateAsset(effect, effectPath);
            }

            // 2. Create the Ability Data
            string abilityPath = $"{FOLDER_PATH}/AbilityData_{name}.asset";
            var abilityData = AssetDatabase.LoadAssetAtPath<TD_AoEApplyEffectAbilityData>(abilityPath);
            if (abilityData == null)
            {
                abilityData = ScriptableObject.CreateInstance<TD_AoEApplyEffectAbilityData>();
                abilityData.abilityName = name;
                abilityData.abilityID = name;
                abilityData.captureRadius = captureRadius;
                abilityData.targetType = targetType;
                abilityData.auraUpdateRate = 0.5f;
                // Auras must be ManualEnd so the coroutine keeps running until the ability is explicitly ended or the character deactivated.
                abilityData.endPolicy = EAbilityEndPolicy.ManualEnd;
                abilityData.effectToApply = effect;

                AssetDatabase.CreateAsset(abilityData, abilityPath);
            }
        }

        private static void EnsureFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Master/TranHuongDao/Core/Abilities"))
                AssetDatabase.CreateFolder("Assets/_Master/TranHuongDao/Core", "Abilities");

            if (!AssetDatabase.IsValidFolder(FOLDER_PATH))
                AssetDatabase.CreateFolder("Assets/_Master/TranHuongDao/Core/Abilities", "Data");
        }
    }
}
#endif
