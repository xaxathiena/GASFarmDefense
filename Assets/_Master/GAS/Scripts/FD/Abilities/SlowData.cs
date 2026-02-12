using UnityEngine;
using GAS;

namespace FD.Abilities
{
    /// <summary>
    /// Data configuration for Slow ability.
    /// Add custom fields here for ability-specific parameters.
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

        public override System.Type GetBehaviourType()
        {
            return typeof(SlowBehaviour);
        }
    }
}
