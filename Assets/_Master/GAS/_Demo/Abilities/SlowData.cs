using UnityEngine;
using Abel.GAS;
using Abel.GAS.Abilities;
using Abel.GAS.Attributes;
using Abel.GAS.Effects;

namespace FD.Abilities
{
    /// <summary>
    /// Data configuration for Slow ability - PURE DATA ONLY.
    /// Add custom fields here for ability-specific parameters.
    /// Behaviour type mapping is handled by GameplayAbilityLogic (auto-detected by convention).
    /// </summary>
    [CreateAssetMenu(fileName = "Slow", menuName = "GAS/Abilities/Slow")]
    public class SlowData : GameplayAbilityData
    {
        // [Header("Slow Settings")]
        // Add your custom fields here
        // Example:
        // public float damage = 50f;
        // public GameObject projectilePrefab;
        // public float projectileSpeed = 20f;
    }
}


