using System.Collections.Generic;
using _Master.Base.Ability;
using UnityEngine;

namespace FD.Character
{
    public class TowerBase : BaseCharacter
    {
        [Header("Abilities")]
        [SerializeField] private GameplayAbility normalAbility;

        [SerializeField] private GameplayAbility skillAbility;

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
                abilitySystemComponent.GiveAbility(normalAbility);
            }

            if (skillAbility != null)
            {
                abilitySystemComponent.GiveAbility(skillAbility);
            }
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
