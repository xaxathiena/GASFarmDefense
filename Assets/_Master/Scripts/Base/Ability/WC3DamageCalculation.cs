using GAS;
using UnityEngine;


namespace FD.Ability
{
    /// <summary>
    /// Warcraft 3 damage calculation for Farm Defense.
    /// Implements the 3-step damage formula:
    /// 1. Base Damage x Critical Multiplier
    /// 2. Apply Type Modifier (Attack Type vs Armor Type)
    /// 3. Apply Armor Reduction
    /// </summary>
    [CreateAssetMenu(fileName = "WC3DamageCalculation", menuName = "FD/Damage Calculation/WC3 Damage Calculation")]
    public class WC3DamageCalculation : DamageCalculationBase
    {
        [Header("Configuration")]
        [Tooltip("Bảng khắc hệ (Attack Type vs Armor Type)")]
        public DamageTypeModifierTable modifierTable;
        
        [Header("Critical Settings")]
        [Tooltip("Enable critical hit calculation")]
        public bool allowCritical = true;
        
        [Header("Debug")]
        [Tooltip("Log detailed damage calculation")]
        public bool debugLog = true;
        
        public override float CalculateMagnitude(
            GameplayEffectContext context,
            AbilitySystemComponent sourceASC,
            AbilitySystemComponent targetASC,
            float baseMagnitude,
            float level)
        {
            // Cast to FD context
            var fdContext = context as FDGameplayEffectContext;
            if (fdContext == null)
            {
                Debug.LogWarning("[WC3DamageCalc] Context is not FDGameplayEffectContext! Using base magnitude.");
                return baseMagnitude;
            }
            
            if (targetASC?.AttributeSet == null)
            {
                Debug.LogWarning("[WC3DamageCalc] Target has no AttributeSet!");
                return baseMagnitude;
            }
            
            float finalDamage = Mathf.Abs(baseMagnitude); // Use absolute value (damage is negative)
            
            // Step 1: Calculate Critical Hit
            float critMultiplier = 1f;
            if (allowCritical && sourceASC?.AttributeSet != null)
            {
                critMultiplier = CalculateCritical(sourceASC, fdContext);
                finalDamage *= critMultiplier;
            }
            
            // Step 2: Apply Type Modifier (Attack Type vs Armor Type)
            float typeModifier = 1f;
            EDamageType damageType = fdContext.DamageType;
            EArmorType armorType = GetTargetArmorType(targetASC);
            
            typeModifier = GetTypeModifier(damageType, armorType);
            fdContext.TypeModifier = typeModifier;
            finalDamage *= typeModifier;
            
            // Step 3: Apply Armor Reduction
            float armorValue = GetTargetArmor(targetASC);
            float armorReduction = CalculateArmorReduction(armorValue);
            fdContext.ArmorReduction = armorReduction;
            finalDamage *= (1f - armorReduction);
            
            // Log if enabled
            if (debugLog)
            {
                string critText = fdContext.IsCriticalHit ? $" [CRIT x{critMultiplier:F1}!]" : "";
                Debug.Log($"<color=red>[WC3 Damage]{critText}</color> " +
                          $"{damageType} vs {armorType} (Armor:{armorValue:F0}) = <color=yellow>{finalDamage:F1}</color>\n" +
                          $"  Base:{Mathf.Abs(baseMagnitude):F0} x Crit:{critMultiplier:F1} x Type:{typeModifier:F2} x (1-Armor:{armorReduction:P0})");
            }
            
            // Return negative for damage
            return -finalDamage;
        }
        
        /// <summary>
        /// Roll for critical hit
        /// </summary>
        private float CalculateCritical(AbilitySystemComponent sourceASC, FDGameplayEffectContext context)
        {
            var critChanceAttr = sourceASC.AttributeSet.GetAttribute(EGameplayAttributeType.CriticalChance);
            var critMultiplierAttr = sourceASC.AttributeSet.GetAttribute(EGameplayAttributeType.CriticalMultiplier);
            
            if (critChanceAttr == null)
                return 1f;
            
            float critChance = Mathf.Clamp01(critChanceAttr.CurrentValue / 100f); // Convert % to 0-1
            float roll = Random.value;
            
            if (roll < critChance)
            {
                float multiplier = critMultiplierAttr?.CurrentValue ?? 2f;
                context.IsCriticalHit = true;
                context.CriticalMultiplier = multiplier;
                return multiplier;
            }
            
            context.IsCriticalHit = false;
            context.CriticalMultiplier = 1f;
            return 1f;
        }
        
        /// <summary>
        /// Get target's armor type from FDAttributeSet
        /// </summary>
        private EArmorType GetTargetArmorType(AbilitySystemComponent targetASC)
        {
            if (targetASC?.AttributeSet is FDAttributeSet fdAttrSet)
            {
                return fdAttrSet.GetArmorType();
            }
            
            return EArmorType.Medium; // Default fallback
        }
        
        /// <summary>
        /// Get target's armor value
        /// </summary>
        private float GetTargetArmor(AbilitySystemComponent targetASC)
        {
            var armorAttr = targetASC.AttributeSet.GetAttribute(EGameplayAttributeType.Armor);
            return armorAttr?.CurrentValue ?? 0f;
        }
        
        /// <summary>
        /// Calculate armor reduction percentage using Warcraft 3 formula
        /// Formula: Reduction = (Armor × 0.06) / (1 + 0.06 × Armor)
        /// 
        /// Examples:
        /// - 10 Armor → 37.5% reduction
        /// - 20 Armor → 54.5% reduction
        /// - 50 Armor → 75.0% reduction
        /// - -10 Armor → -46.2% reduction (increases damage by 46.2%)
        /// </summary>
        private float CalculateArmorReduction(float armor)
        {
            return (armor * 0.06f) / (1f + 0.06f * armor);
        }
        
        /// <summary>
        /// Get type modifier from table
        /// </summary>
        private float GetTypeModifier(EDamageType damageType, EArmorType armorType)
        {
            if (modifierTable == null)
            {
                Debug.LogWarning("[WC3DamageCalc] No modifier table assigned! Using 1.0");
                return 1f;
            }
            
            return modifierTable.GetModifier(damageType, armorType);
        }
    }
}
