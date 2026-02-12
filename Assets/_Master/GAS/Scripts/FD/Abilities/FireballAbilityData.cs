using UnityEngine;
using GAS;

namespace FD.Abilities
{
    /// <summary>
    /// Data configuration for Fireball ability.
    /// This is a ScriptableObject that designers can create and configure in Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "Fireball", menuName = "GAS/Abilities/Fireball")]
    public class FireballAbilityData : GameplayAbilityData
    {
        [Header("Fireball Settings")]
        public float damage = 50f;
        public GameObject projectilePrefab;
        public float projectileSpeed = 20f;
        public float projectileLifetime = 3f;
        
        [Header("Visual Effects")]
        public GameObject castEffect;
        public GameObject hitEffect;

        public override System.Type GetBehaviourType()
        {
            return typeof(FireballAbilityBehaviour);
        }
    }
}
