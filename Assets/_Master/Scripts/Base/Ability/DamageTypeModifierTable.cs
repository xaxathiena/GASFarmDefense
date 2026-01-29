using System.Collections.Generic;
using UnityEngine;

namespace FD.Ability
{
    /// <summary>
    /// Bảng khắc hệ cho hệ thống damage Warcraft 3.
    /// Định nghĩa damage modifier dựa trên Attack Type vs Armor Type.
    /// </summary>
    [CreateAssetMenu(fileName = "DamageTypeTable", menuName = "FD/Damage Calculation/Damage Type Modifier Table")]
    public class DamageTypeModifierTable : ScriptableObject
    {
        [System.Serializable]
        public class TypeModifierEntry
        {
            [Tooltip("Loại damage (từ ability)")]
            public EDamageType attackType;
            
            [Tooltip("Loại giáp (từ character)")]
            public EArmorType armorType;
            
            [Range(0f, 3f)]
            [Tooltip("Hệ số nhân damage (0.35 = 35%, 1.0 = 100%, 2.0 = 200%)")]
            public float modifier = 1f;
        }
        
        [Header("Type Modifier Table")]
        [Tooltip("Bảng khắc hệ theo Warcraft 3")]
        public List<TypeModifierEntry> modifiers = new List<TypeModifierEntry>();
        
        /// <summary>
        /// Get modifier for attack type vs armor type combination
        /// </summary>
        public float GetModifier(EDamageType attackType, EArmorType armorType)
        {
            var entry = modifiers.Find(x => 
                x.attackType == attackType && x.armorType == armorType);
            
            if (entry != null)
                return entry.modifier;
            
            // Default to 1.0 (100% damage) if not found
            Debug.LogWarning($"No modifier found for {attackType} vs {armorType}, using 1.0");
            return 1f;
        }
        
        /// <summary>
        /// Initialize table with default Warcraft 3 values
        /// </summary>
        [ContextMenu("Initialize Default WC3 Table")]
        public void InitializeDefaultTable()
        {
            modifiers.Clear();
            
            // Normal vs...
            AddModifier(EDamageType.Normal, EArmorType.Light, 1.0f);
            AddModifier(EDamageType.Normal, EArmorType.Medium, 1.5f);
            AddModifier(EDamageType.Normal, EArmorType.Heavy, 1.0f);
            AddModifier(EDamageType.Normal, EArmorType.Fortified, 0.5f);
            AddModifier(EDamageType.Normal, EArmorType.Hero, 1.0f);
            AddModifier(EDamageType.Normal, EArmorType.Unarmored, 1.0f);
            
            // Pierce vs...
            AddModifier(EDamageType.Pierce, EArmorType.Light, 2.0f);
            AddModifier(EDamageType.Pierce, EArmorType.Medium, 0.75f);
            AddModifier(EDamageType.Pierce, EArmorType.Heavy, 0.35f);
            AddModifier(EDamageType.Pierce, EArmorType.Fortified, 0.35f);
            AddModifier(EDamageType.Pierce, EArmorType.Hero, 0.5f);
            AddModifier(EDamageType.Pierce, EArmorType.Unarmored, 2.0f);
            
            // Siege vs...
            AddModifier(EDamageType.Siege, EArmorType.Light, 0.5f);
            AddModifier(EDamageType.Siege, EArmorType.Medium, 1.0f);
            AddModifier(EDamageType.Siege, EArmorType.Heavy, 1.0f);
            AddModifier(EDamageType.Siege, EArmorType.Fortified, 1.5f);
            AddModifier(EDamageType.Siege, EArmorType.Hero, 0.5f);
            AddModifier(EDamageType.Siege, EArmorType.Unarmored, 1.0f);
            
            // Magic vs...
            AddModifier(EDamageType.Magic, EArmorType.Light, 1.0f);
            AddModifier(EDamageType.Magic, EArmorType.Medium, 0.75f);
            AddModifier(EDamageType.Magic, EArmorType.Heavy, 2.0f);
            AddModifier(EDamageType.Magic, EArmorType.Fortified, 0.35f);
            AddModifier(EDamageType.Magic, EArmorType.Hero, 0.5f);
            AddModifier(EDamageType.Magic, EArmorType.Unarmored, 1.0f);
            AddModifier(EDamageType.Magic, EArmorType.MagicImmune, 0f); // Immune
            
            // Chaos (always 100%)
            foreach (EArmorType armor in System.Enum.GetValues(typeof(EArmorType)))
            {
                AddModifier(EDamageType.Chaos, armor, 1.0f);
            }
            
            // Hero vs...
            AddModifier(EDamageType.Hero, EArmorType.Light, 1.0f);
            AddModifier(EDamageType.Hero, EArmorType.Medium, 1.0f);
            AddModifier(EDamageType.Hero, EArmorType.Heavy, 1.0f);
            AddModifier(EDamageType.Hero, EArmorType.Fortified, 0.5f);
            AddModifier(EDamageType.Hero, EArmorType.Hero, 1.0f);
            AddModifier(EDamageType.Hero, EArmorType.Unarmored, 1.0f);
            
            Debug.Log($"[DamageTypeTable] Initialized with {modifiers.Count} entries");
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
        
        private void AddModifier(EDamageType attack, EArmorType armor, float modifier)
        {
            modifiers.Add(new TypeModifierEntry
            {
                attackType = attack,
                armorType = armor,
                modifier = modifier
            });
        }
    }
}
