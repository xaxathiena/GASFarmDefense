using FD.Ability;
using FD.Data;
using GAS;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace FD
{
    public readonly struct EventTowerDestroyed
    {
        public readonly string TowerId;
        public readonly Vector3 Position;
        public EventTowerDestroyed(string towerId, Vector3 position)
        {
            TowerId = towerId;
            Position = position;
        }
    }
    public class TowerController
    {
        // TowerController implementation
        private readonly AbilitySystemComponent asc;
        private readonly IDebugService debug;
        private readonly IEventBus eventBus;
        private readonly IPoolManager poolManager;
        private readonly EnemyManager enemyManager;
        private readonly FDAttributeSet attributeSet = new FDAttributeSet();
        private TowerData towerData;
        private TowerView towerView;
        private string id;
        private bool isShow = false;
        
        // Performance profiler
        private ASCPerformanceProfiler profiler;

        // Ability management
        private List<GameplayAbilitySpec> abilitySpecs = new List<GameplayAbilitySpec>();

        private float nextTargetUpdateTime;

        public string Id => id;
        
#if UNITY_EDITOR
        // Public API for Editor debug tools
        public AbilitySystemComponent AbilitySystemComponent => asc;
        public ASCPerformanceProfiler Profiler => profiler;
        
        public Transform Transform => towerView?.transform;
        public string DisplayName => $"Tower #{currentCount} ({id.Substring(0, 8)})";
#endif
        public TowerController(IDebugService debug,
        AbilitySystemComponent asc,
        IEventBus eventBus,
        IPoolManager poolManager,
        EnemyManager enemyManager)
        {
            this.debug = debug;
            this.asc = asc;
            this.eventBus = eventBus;
            this.poolManager = poolManager;
            this.enemyManager = enemyManager;
            id = System.Guid.NewGuid().ToString();
        }

        public void OnSetup(TowerView towerView, TowerData towerData)
        {
            this.towerData = towerData;
            this.towerView = towerView;
            currentCount = ++count;
            this.towerView.ownerASC = this.asc; // Set owner for AbilitySystemComponent access
            this.asc.InitOwner(this.towerView.transform); // Set owner later if needed
            this.asc.InitializeAttributeSet(attributeSet);
            
            // Initialize profiler
            profiler = new ASCPerformanceProfiler(asc, $"Tower_{currentCount}");
            
            GrantAbilities();
        }
        private static int count;
        private int currentCount;
        public void Tick()
        {
            isShow = true;
            
            profiler?.BeginTick();
            asc.Tick();
            profiler?.EndTick();
            
            TryActivateAbilities();
        }
        public void Destroy()
        {
            debug.Log($"TowerController {id} is being destroyed!", Color.red);
            poolManager.Despawn(towerView); // Pass the actual TowerView instance if available˝
            eventBus.Publish(new EventTowerDestroyed(id, Vector3.zero)); // You can set the actual position if needed
        }

        private void GrantAbilities()
        {
            if (asc == null || towerData == null || towerData.Abilities == null)
            {
                return;
            }

            abilitySpecs.Clear();
            foreach (var abilityInit in towerData.Abilities)
            {
                if (abilityInit.ability != null)
                {
                    var spec = asc.GiveAbility(abilityInit.ability, Mathf.Max(1, abilityInit.level));
                    abilitySpecs.Add(spec);
                }
            }

            // Activate passive abilities first (auras, buffs, etc.)
            // These don't require targets and should be reactivated after cooldown
            foreach (var abilityInit in towerData.Abilities)
            {
                if (abilityInit.isPassive && abilityInit.ability != null && CanActivateAbility(abilityInit.ability))
                {
                    asc.TryActivateAbility(abilityInit.ability);
                }
            }
        }

        public void UpgradeAbility(int abilityIndex, int deltaLevel)
        {
            if (deltaLevel == 0 || asc == null || abilityIndex < 0)
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
            if (towerData == null || towerData.Abilities == null || asc == null)
            {
                return;
            }

            // Check if can perform actions (not stunned, not disabled, not silenced)
            if (asc.HasAnyTags(GameplayTag.State_Disabled, GameplayTag.State_Silenced, GameplayTag.State_Stunned))
            {
                return;
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
                if (!abilityInit.isPassive)
                {
                    continue;
                }

                // Check if ability can be activated (off cooldown and has enough mana)
                if (CanActivateAbility(abilityInit.ability))
                {
                    asc.TryActivateAbility(abilityInit.ability);
                }
            }
        }

        private bool CanActivateAbility(GameplayAbilityData ability)
        {
            if (ability == null || asc == null)
            {
                return false;
            }

            if (asc.IsAbilityOnCooldown(ability))
            {
                return false;
            }

            return true;
        }

        private List<Transform> GetTargets()
        {
            var targets = new List<Transform>();
            if (towerData == null || towerView == null || towerData.TargetRange <= 0f)
            {
                return targets;
            }

            // Use EnemyManager service for distance-based queries (no Physics overhead)
            var candidateBuffer = enemyManager.GetEnemiesInRange(towerView.transform.position, towerData.TargetRange, towerData.TargetLayerMask);

            if (candidateBuffer.Count == 0)
            {
                return targets;
            }

            // Sort by distance (closest first) - using sqrMagnitude to avoid sqrt
            Vector3 pos = towerView.transform.position;
            candidateBuffer.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                float da = (a.position - pos).sqrMagnitude;
                float db = (b.position - pos).sqrMagnitude;
                return da.CompareTo(db);
            });

            int limit = towerData.MaxTargets <= 0 ? candidateBuffer.Count : Mathf.Min(towerData.MaxTargets, candidateBuffer.Count);
            for (int i = 0; i < limit; i++)
            {
                targets.Add(candidateBuffer[i]);
            }

            return targets;
        }
    }
}