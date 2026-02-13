using System.Collections.Generic;
using GAS;
using UnityEngine;

namespace FD.Character
{
    public class TowerBase : BaseCharacter
    {
        

        [Header("Abilities")]
        [SerializeField] private List<AbilityInit> abilities = new List<AbilityInit>();
        private List<GameplayAbilitySpec> abilitySpecs = new List<GameplayAbilitySpec>();

        [Header("Targeting")]
        [SerializeField] private float targetRange = 6f;
        [SerializeField] private LayerMask targetLayerMask = ~0;
        [SerializeField] private int maxTargets = 1;

        [Header("Gizmos")]
        [SerializeField] private Color rangeGizmoColor = new Color(0.2f, 0.8f, 1f, 0.2f);
        [SerializeField] private Color targetLineColor = new Color(1f, 0.4f, 0.1f, 0.9f);

        [Header("Performance")]
        [SerializeField] private float targetUpdateInterval = 0.2f; // Update targets 5 times per second

        private List<Transform> cachedTargets = new List<Transform>();
        private float nextTargetUpdateTime;

        protected override void InitializeAttributeSet()
        {
            attributeSet.MoveSpeed.BaseValue = 10f;
            attributeSet.MaxHealth.BaseValue = 200f;
            attributeSet.Health.BaseValue = attributeSet.MaxHealth.BaseValue;
            attributeSet.Armor.BaseValue = 5f;
            attributeSet.Mana.BaseValue = 100f;
            attributeSet.MaxMana.BaseValue = 100f;
            attributeSet.ManaRegen.BaseValue = 2f; // 2 mana per
            attributeSet.CriticalChance.BaseValue = 0.1f; // 10% crit chance
            attributeSet.CriticalMultiplier.BaseValue = 2f; // 2x crit damage
            attributeSet.BaseDamage.BaseValue = 15f;
        }
        void Start()
        {
            GrantAbilities();
        }
        protected override void Update()
        {
            base.Update();
            TryActivateAbilities();
        }

        private void GrantAbilities()
        {
            if (abilitySystemComponent == null)
            {
                return;
            }

            abilitySpecs.Clear();
            foreach (var abilityInit in abilities)
            {
                if (abilityInit.ability != null)
                {
                    var spec = abilitySystemComponent.GiveAbility(abilityInit.ability, Mathf.Max(1, abilityInit.level));
                    abilitySpecs.Add(spec);
                }
            }
            // Activate passive abilities first (auras, buffs, etc.)
            // These don't require targets and should be reactivated after cooldown
            foreach (var abilityInit in abilities)
            {
                if (abilityInit.isPassive && abilityInit.ability != null && CanActivateAbility(abilityInit.ability))
                {
                    abilitySystemComponent.TryActivateAbility(abilityInit.ability);
                }
            }
        }

        public void UpgradeAbility(int abilityIndex, int deltaLevel)
        {
            if (deltaLevel == 0 || abilitySystemComponent == null || abilityIndex < 0)
            {
                return;
            }

            if (abilityIndex >= abilitySpecs.Count)
            {
                return;
            }

            abilitySpecs[abilityIndex]?.AddLevels(deltaLevel);
        }

        private void TryActivateAbilities()
        {
            // Check if can perform actions (not stunned, not disabled, etc.)
            if (!CanPerformActions())
            {
                return;
            }

            // Get targets first - but only update periodically
            if (Time.time >= nextTargetUpdateTime)
            {
                cachedTargets = GetTargets();
                nextTargetUpdateTime = Time.time + targetUpdateInterval;
            }

            // Try to activate each ability that can be activated
            foreach (var abilityInit in abilities)
            {
                if (abilityInit.ability == null)
                {
                    continue;
                }

                // Passive abilities can activate without targets
                // Non-passive abilities need targets
                if (abilityInit.isPassive)
                {
                    continue;
                }
                if (cachedTargets == null || cachedTargets.Count == 0)
                {
                    continue;
                }

                // Check if ability can be activated (off cooldown and has enough mana)
                if (CanActivateAbility(abilityInit.ability))
                {
                    abilitySystemComponent.TryActivateAbility(abilityInit.ability);
                }
            }
        }

        private bool CanActivateAbility(GameplayAbilityData ability)
        {
            if (ability == null || abilitySystemComponent == null)
            {
                return false;
            }

            if (abilitySystemComponent.IsAbilityOnCooldown(ability))
            {
                return false;
            }

            return true;
        }

        // Reusable buffers to avoid allocations
        private static List<Transform> candidateBuffer = new List<Transform>(50);

        public override List<Transform> GetTargets()
        {
            var targets = new List<Transform>();
            if (targetRange <= 0f)
            {
                return targets;
            }

            // Use EnemyManager for distance-based queries (no Physics overhead)
            candidateBuffer = EnemyManager.GetEnemiesInRange(transform.position, targetRange, targetLayerMask);
            
            if (candidateBuffer.Count == 0)
            {
                return targets;
            }

            // Sort by distance (closest first) - using sqrMagnitude to avoid sqrt
            Vector3 pos = transform.position;
            candidateBuffer.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                float da = (a.position - pos).sqrMagnitude;
                float db = (b.position - pos).sqrMagnitude;
                return da.CompareTo(db);
            });

            int limit = maxTargets <= 0 ? candidateBuffer.Count : Mathf.Min(maxTargets, candidateBuffer.Count);
            for (int i = 0; i < limit; i++)
            {
                targets.Add(candidateBuffer[i]);
            }

            return targets;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = rangeGizmoColor;
            Gizmos.DrawWireSphere(transform.position, targetRange);

            Gizmos.color = targetLineColor;
            var targets = Application.isPlaying ? cachedTargets : GetTargets();
            if (targets == null)
            {
                return;
            }

            foreach (var target in targets)
            {
                if (target != null)
                {
                    Gizmos.DrawLine(transform.position, target.position);
                }
            }
        }
    }
}
