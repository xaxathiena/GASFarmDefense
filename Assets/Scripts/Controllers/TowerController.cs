using System;
using System.Collections.Generic;
using FD.Data;
using FD.Services;
using FD.Views;
using GAS;
using UnityEngine;
using VContainer.Unity;

namespace FD.Controllers
{
    public class TowerController : IDisposable, ITickable, IStartable
    {
        private readonly TowerView towerView;
        private readonly AbilitySystemComponent acs;
        public readonly IEnemyRegistry enemyRegistry;
        private List<GameplayAbilitySpec> abilitySpecs = new List<GameplayAbilitySpec>();
        private TowerData towerData;
        private IReadOnlyList<IEnemy> cachedTargets;
        [SerializeField] private float targetUpdateInterval = 0.2f; // Update targets 5 times per second


        public TowerController(TowerView towerView, TowerData towerData, AbilitySystemComponent acs, IEnemyRegistry enemyRegistry)
        {
            this.towerData = towerData;
            this.towerView = towerView;
            this.acs = acs;
            this.enemyRegistry = enemyRegistry;
        }
        public void Dispose()
        {

        }

        public void Start()
        {
            GrantAbilities();
        }

        public void Tick()
        {
            TryActivateAbilities();
        }
        private void GrantAbilities()
        {
            if (acs == null)
            {
                return;
            }

            abilitySpecs.Clear();
            foreach (var abilityInit in towerData.Abilities)
            {
                if (abilityInit.ability != null)
                {
                    var spec = acs.GiveAbility(abilityInit.ability, Mathf.Max(1, abilityInit.level));
                    abilitySpecs.Add(spec);
                }
            }
            // Activate passive abilities first (auras, buffs, etc.)
            // These don't require targets and should be reactivated after cooldown
            foreach (var abilityInit in towerData.Abilities)
            {
                if (abilityInit.isPassive && abilityInit.ability != null && CanActivateAbility(abilityInit.ability))
                {
                    acs.TryActivateAbility(abilityInit.ability);
                }
            }
        }

        private bool CanActivateAbility(GameplayAbilityData ability)
        {
            if (ability == null || acs == null)
            {
                return false;
            }

            if (acs.IsAbilityOnCooldown(ability))
            {
                return false;
            }

            return true;
        }
        private float nextTargetUpdateTime;
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
            foreach (var abilityInit in towerData.Abilities)
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
                    acs.TryActivateAbility(abilityInit.ability);
                }
            }
        }

        private IReadOnlyList<IEnemy> GetTargets()
        {
            return enemyRegistry.GetEnemiesInRange(towerView.transform.position, towerData.TargetRange, towerData.TargetLayerMask);
        }

        public virtual bool CanPerformActions()
        {
            if (IsStunned())
            {
                return false;
            }

            // Check other blocking states
            if (acs.HasAnyTags(GameplayTag.State_Disabled, GameplayTag.State_Silenced))
            {
                return false;
            }

            return true;
        }
        public bool IsStunned()
        {
            return acs.HasAnyTags(GameplayTag.State_Stunned);
        }
    }
}