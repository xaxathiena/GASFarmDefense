using GAS;
using UnityEngine;
using System.Collections.Generic;

namespace FD.Ability
{
    /// <summary>
    /// Area of Effect ability that slows enemies within radius
    /// </summary>
    [CreateAssetMenu(fileName = "MoveSlowAura", menuName = "GAS/Abilities/Move Slow Aura")]
    public class MoveSlowAuraAbility : FDGameplayAbility
    {
        [Header("Aura Settings")]
        [Tooltip("Radius of the slow aura")]
        public float auraRadius = 5f;
        
        [Tooltip("How long the aura lasts (0 = infinite)")]
        public float auraDuration = 10f;
        
        [Tooltip("Visual effect prefab for the aura")]
        public GameObject auraPrefab;
        
        [Tooltip("Should the aura follow the caster?")]
        public bool followCaster = false;
        
        [Header("Slow Effect")]
        [Tooltip("GameplayEffect that applies the slow")]
        public GameplayEffect slowEffect;
         [Tooltip("Layer mask for enemy detection")]
        [SerializeField] private LayerMask enemyLayerMask = ~0;
        protected override void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var owner = GetAbilityOwner(asc);
            if (owner == null)
            {
                Debug.LogWarning("MoveSlowAura: No owner found");
                EndAbility(asc);
                return;
            }

            if (slowEffect == null)
            {
                Debug.LogError("MoveSlowAura: No slow effect assigned!");
                EndAbility(asc);
                return;
            }

            // Spawn aura at owner's position
            Vector3 spawnPos = owner.transform.position;
            GameObject auraObj;
            
            if (auraPrefab != null)
            {
                auraObj = Object.Instantiate(auraPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Create default aura object
                auraObj = new GameObject($"MoveSlowAura_{owner.name}");
                auraObj.transform.position = spawnPos;
            }

            // Setup aura detector component
            var detector = auraObj.GetComponent<AuraDetector>();
            if (detector == null)
            {
                detector = auraObj.AddComponent<AuraDetector>();
            }
            
            // Initialize the detector
            detector.Initialize(
                auraRadius, 
                auraDuration, 
                slowEffect, 
                asc,
                follow: followCaster ? owner.transform : null,
                enemyLayer: enemyLayerMask
            );
            
            Debug.Log($"MoveSlowAura activated at {spawnPos} with radius {auraRadius}");
            
            EndAbility(asc);
        }
    }
}
