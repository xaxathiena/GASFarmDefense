using UnityEngine;
using GAS;

namespace FD.Abilities
{
    /// <summary>
    /// Data configuration for Fireball ability - PURE DATA ONLY.
    /// This is a ScriptableObject that designers can create and configure in Unity Editor.
    /// Behaviour type mapping is handled by GameplayAbilityLogic.
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
    }
}
