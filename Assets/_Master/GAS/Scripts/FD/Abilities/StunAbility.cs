using FD.Character;
using GAS;
using UnityEngine;

namespace FD.Ability
{
    /// <summary>
    /// Stun ability for Farm Defense game
    /// Applies stun effect that prevents target from taking any actions
    /// Checks for immunity before applying
    /// </summary>
    [CreateAssetMenu(fileName = "StunAbility", menuName = "FD/Abilities/Stun Ability")]
    public class StunAbility : FDGameplayAbility
    {
        [Header("Stun Configuration")]
        [Tooltip("Stun effect to apply (should have 'State.Stunned' tag)")]
        [SerializeField] private GameplayEffect stunEffect;

        [Tooltip("Chance to stun target (0-1)")]
        [SerializeField] private float stunChance = 0.5f;

        [Tooltip("Duration of stun in seconds")]
        [SerializeField] private float stunDuration = 2f;

        [Tooltip("Visual effect prefab when target is stunned (optional)")]
        [SerializeField] private GameObject stunVFXPrefab;

        [Header("Damage Configuration")]
        [Tooltip("Damage effect to apply")]
        [SerializeField] private GameplayEffect damageEffect;

        protected override void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            base.OnAbilityActivated(asc, spec);

            var owner = GetAbilityOwner(asc);
            if (owner == null)
            {
                EndAbility(asc, spec);
                return;
            }

            // Get BaseCharacter component
            var character = owner.GetComponent<BaseCharacter>();
            if (character == null)
            {
                Debug.LogWarning($"[StunAbility] {owner.name} doesn't have BaseCharacter component!");
                EndAbility(asc, spec);
                return;
            }

            // Get targets
            var targets = character.GetTargets();
            if (targets == null || targets.Count == 0)
            {
                Debug.LogWarning($"[StunAbility] No targets found for {owner.name}");
                EndAbility(asc, spec);
                return;
            }

            // Apply damage and stun to each target
            foreach (var target in targets)
            {
                var targetASC = target.GetComponent<AbilitySystemComponent>();
                if (targetASC == null)
                {
                    continue;
                }

                // Apply damage first
                if (damageEffect != null)
                {
                    ApplyEffectWithContext(damageEffect, asc, targetASC, spec);
                }

                // Roll for stun chance
                float roll = Random.Range(0f, 1f);
                if (roll <= stunChance)
                {
                    // Check if target is immune to stun
                    if (IsImmune(targetASC))
                    {
                        Debug.Log($"[StunAbility] {target.name} is IMMUNE to stun!");
                        OnStunBlocked(target, targetASC);
                        continue;
                    }

                    // Apply stun effect
                    if (stunEffect != null)
                    {
                        ApplyEffectWithContext(stunEffect, asc, targetASC, spec);
                        Debug.Log($"[StunAbility] {target.name} is STUNNED for {stunDuration}s!");
                        OnStunApplied(target, targetASC, null);
                    }
                }
            }

            EndAbility(asc, spec);
        }

        /// <summary>
        /// Check if target is immune to stun
        /// Checks for "State.Immune" or "State.Immune.Stun" tags
        /// </summary>
        private bool IsImmune(AbilitySystemComponent targetASC)
        {
            // Check for general immunity tag
            if (targetASC.HasAnyTags(GameplayTag.State_Immune, GameplayTag.State_Immune_Stun, GameplayTag.State_Immune_CC))
            {
                return true;
            }

            // Check FD-specific immunity via attribute
            var fdAttributeSet = targetASC.GetAttributeSet<FDAttributeSet>();
            if (fdAttributeSet != null)
            {
                // Check if has Immunity attribute (if implemented)
                var immunityAttr = fdAttributeSet.GetAttribute("Immunity");
                if (immunityAttr != null && immunityAttr.CurrentValue > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Called when stun is successfully applied
        /// Override to add custom logic (VFX, sounds, etc.)
        /// </summary>
        protected virtual void OnStunApplied(Transform target, AbilitySystemComponent targetASC, ActiveGameplayEffect activeStunEffect)
        {
            // Spawn VFX if available
            if (stunVFXPrefab != null)
            {
                var vfx = Instantiate(stunVFXPrefab, target.position, Quaternion.identity, target);
                
                // Destroy VFX after stun duration
                Destroy(vfx, stunDuration);
            }
        }

        /// <summary>
        /// Called when stun is blocked by immunity
        /// Override to add custom logic (show immunity VFX, etc.)
        /// </summary>
        protected virtual void OnStunBlocked(Transform target, AbilitySystemComponent targetASC)
        {
            // Override in subclass to show "IMMUNE" popup or VFX
        }
    }
}
