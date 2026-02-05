using System.Collections.Generic;
using GAS;
using UnityEngine;

namespace FD.Character
{
    public class TowerBase : BaseCharacter
    {
        [System.Serializable]
        public class AbilityInit
        {
            public GameplayAbility ability;
            public int level = 1;
            [Tooltip("Passive abilities will be continuously activated (e.g., aura effects)")]
            public bool isPassive = false;
        }

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

        private List<Transform> cachedTargets = new List<Transform>();

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

            

            // For active abilities, only activate if we have targets
            cachedTargets = GetTargets();
            if (cachedTargets == null || cachedTargets.Count == 0)
            {
                return;
            }

            // Try to activate each active ability that can be activated
            foreach (var abilityInit in abilities)
            {
                if (abilityInit.isPassive && abilityInit.ability != null && CanActivateAbility(abilityInit.ability))
                {
                    abilitySystemComponent.TryActivateAbility(abilityInit.ability);
                }
            }
        }

        private bool CanActivateAbility(GameplayAbility ability)
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

        public override List<Transform> GetTargets()
        {
            var targets = new List<Transform>();
            if (targetRange <= 0f)
            {
                return targets;
            }

            var colliders = Physics.OverlapSphere(transform.position, targetRange, targetLayerMask);
            if (colliders == null || colliders.Length == 0)
            {
                return targets;
            }

            var candidates = new List<Transform>();
            foreach (var col in colliders)
            {
                if (col == null)
                {
                    continue;
                }

                var enemy = col.GetComponentInParent<EnemyBase>();
                if (enemy != null)
                {
                    candidates.Add(enemy.transform);
                }
            }

            candidates.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                float da = Vector3.Distance(transform.position, a.position);
                float db = Vector3.Distance(transform.position, b.position);
                return da.CompareTo(db);
            });

            int limit = maxTargets <= 0 ? candidates.Count : Mathf.Min(maxTargets, candidates.Count);
            for (int i = 0; i < limit; i++)
            {
                targets.Add(candidates[i]);
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
