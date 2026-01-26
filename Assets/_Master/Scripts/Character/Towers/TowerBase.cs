using System.Collections.Generic;
using GAS;
using UnityEngine;

namespace FD.Character
{
    public class TowerBase : BaseCharacter
    {
        [Header("Abilities")]
        [SerializeField] private GameplayAbility normalAbility;
        [SerializeField] private int normalAbilityLevel = 1;

        [SerializeField] private GameplayAbility skillAbility;
        [SerializeField] private int skillAbilityLevel = 1;
        private GameplayAbilitySpec normalAbilitySpec;
        private GameplayAbilitySpec skillAbilitySpec;

        [Header("Targeting")]
        [SerializeField] private float targetRange = 6f;
        [SerializeField] private LayerMask targetLayerMask = ~0;
        [SerializeField] private int maxTargets = 1;

        [Header("Gizmos")]
        [SerializeField] private Color rangeGizmoColor = new Color(0.2f, 0.8f, 1f, 0.2f);
        [SerializeField] private Color targetLineColor = new Color(1f, 0.4f, 0.1f, 0.9f);

        private List<Transform> cachedTargets = new List<Transform>();

        protected override void Start()
        {
            base.Start();
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

            if (normalAbility != null)
            {
                normalAbilitySpec = abilitySystemComponent.GiveAbility(normalAbility, Mathf.Max(1, normalAbilityLevel));
            }

            if (skillAbility != null)
            {
                skillAbilitySpec = abilitySystemComponent.GiveAbility(skillAbility, Mathf.Max(1, skillAbilityLevel));
            }
        }

        public void UpgradeNormalAbility(int deltaLevel)
        {
            if (deltaLevel == 0 || abilitySystemComponent == null)
            {
                return;
            }

            if (normalAbilitySpec == null && normalAbility != null)
            {
                normalAbilitySpec = abilitySystemComponent.GiveAbility(normalAbility, Mathf.Max(1, normalAbilityLevel));
            }

            normalAbilitySpec?.AddLevels(deltaLevel);
        }

        public void UpgradeSkillAbility(int deltaLevel)
        {
            if (deltaLevel == 0 || abilitySystemComponent == null)
            {
                return;
            }

            if (skillAbilitySpec == null && skillAbility != null)
            {
                skillAbilitySpec = abilitySystemComponent.GiveAbility(skillAbility, Mathf.Max(1, skillAbilityLevel));
            }

            skillAbilitySpec?.AddLevels(deltaLevel);
        }

        private void TryActivateAbilities()
        {
            cachedTargets = GetTargets();
            if (cachedTargets == null || cachedTargets.Count == 0)
            {
                return;
            }

            bool skillActivated = skillAbility != null && TryActivateSkill();
            if (!skillActivated)
            {
                TryActivateNormal();
            }
        }

        private bool TryActivateSkill()
        {
            if (skillAbility == null || abilitySystemComponent == null)
            {
                return false;
            }

            if (abilitySystemComponent.IsAbilityOnCooldown(skillAbility))
            {
                return false;
            }

            return abilitySystemComponent.TryActivateAbility(skillAbility);
        }

        private bool TryActivateNormal()
        {
            if (normalAbility == null || abilitySystemComponent == null)
            {
                return false;
            }

            if (abilitySystemComponent.IsAbilityOnCooldown(normalAbility))
            {
                return false;
            }

            return abilitySystemComponent.TryActivateAbility(normalAbility);
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
