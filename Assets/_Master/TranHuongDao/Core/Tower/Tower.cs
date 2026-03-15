using System;
using UnityEngine;
using GAS;
using Abel.TranHuongDao.Core.Abilities;
using Abel.TranHuongDao.Core.VFX;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Pure C# tower entity.  No MonoBehaviour — rendered via IRender2DService.
    ///
    /// Lifecycle:
    ///   1. new Tower(asc)         — resolved by VContainer (Transient ASC)
    ///   2. Initialize(...)        — configures position, GAS, ability, render
    ///   3. Tick(dt)               — called every frame by TowerManager (ITickable)
    ///   4. Cleanup()              — called by TowerManager on removal
    /// </summary>
    public class Tower : IGASAvatar
    {
        private readonly AbilitySystemComponent _asc;
        public AbilitySystemComponent ASC => _asc;

        // ── Identity ─────────────────────────────────────────────────────────────
        public int InstanceID { get; private set; }
        public string TowerID { get; private set; }
        
        // --- IGASAvatar Implementation ---
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public Vector3 Scale => Vector3.one;
        public bool IsValid => true;

        // ── GAS ──────────────────────────────────────────────────────────────────
        private readonly AbilityBehaviourRegistry _behaviourRegistry;
        private readonly UnitAttributeSet _attributeSet = new UnitAttributeSet();

        // The attack ability data resolved from AbilitiesConfig at spawn time.
        // Typed as the base class so any GameplayAbilityData subtype can be granted.
        private GameplayAbilityData attackAbilityData;

        // The skill ability data resolved from AbilitiesConfig at spawn time.
        // Null for units that have no skill configured in UnitConfig.SkillAbilityID.
        private GameplayAbilityData skillAbilityData;

        // ── Render ───────────────────────────────────────────────────────────────
        private IRender2DService renderService;
        private FD.IEventBus _eventBus;
        private bool renderInitialized;

        // ── VFX ──────────────────────────────────────────────────────────────────
        private StatusEffectVFXController vfxController;

        // ── Proxy Transform (needed by ASC.GetOwner() for ability range checks) ──
        // A lightweight GameObject created once and destroyed on Cleanup().
        private GameObject proxyGO;

        /// <summary>
        /// The transform of the proxy GameObject created in Initialize().
        /// External systems (e.g. TowerManager) may attach colliders or components to it.
        /// </summary>
        public Transform ProxyTransform => proxyGO != null ? proxyGO.transform : null;

        // ── Events ───────────────────────────────────────────────────────────────
        public event Action<Tower> OnDestroyed;

        // ── Attack ROF Timer ──────────────────────────────────────────────────────
        // Tracks seconds remaining until the next attack. Managed entirely by Tower;
        // does NOT use GAS cooldown so the GAS cooldown is free for skills.
        private float _attackTimer;

        // Cached max-health inverse: avoids a division every time HP changes.
        private float maxHealthInverse;

        // ─────────────────────────────────────────────────────────────────────────
        public Tower(AbilitySystemComponent asc, AbilityBehaviourRegistry behaviourRegistry)
        {
            _asc = asc;
            _behaviourRegistry = behaviourRegistry;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        public void Initialize(
            int instanceID,
            string towerID,
            Vector3 position,
            UnitConfig config,
            GameplayAbilityData attackAbilityData,
            GameplayAbilityData skillAbilityData,
            IRender2DService renderService,
            FD.IEventBus eventBus,
            FD.Modules.VFX.IVFXManager vfxManager,
            TagVFXConfig vfxConfig)
        {
            InstanceID = instanceID;
            TowerID = towerID;
            Position = position;
            this.attackAbilityData = attackAbilityData;
            this.skillAbilityData = skillAbilityData;
            this.renderService = renderService;

            // ── Proxy Transform ────────────────────────────────────────────────
            // No longer needed by GAS, but kept for Colliders/Selection if needed.
            proxyGO = new GameObject($"TowerProxy_{instanceID}");
            proxyGO.transform.position = position;
            
            // Link GAS to this Tower as the spatial avatar
            _asc.InitAvatar(this);

            // ── GAS setup ──────────────────────────────────────────────────────
            // Seed all GAS attributes from the authored balance config.
            _attributeSet.InitializeFromConfig(config);
            _asc.InitializeAttributeSet(_attributeSet);

            // Cache the reciprocal once so HP changes only cost a multiply, not a divide.
            maxHealthInverse = config.MaxHealth > 0f ? 1f / config.MaxHealth : 1f;

            // Subscribe so HP changes are forwarded to the render pipeline automatically.
            _attributeSet.Health.OnValueChanged += HandleHealthValueChanged;

            // Grant the attack ability so the ASC can activate it.
            if (attackAbilityData != null)
            {
                var spec = _asc.GiveAbility(attackAbilityData);
                spec.cooldownRateAttr = EGameplayAttributeType.NormalCooldownRate;

                // Auto-activate if policy is OnGranted (e.g. passive/aura)
                if (attackAbilityData.activationPolicy == EAbilityActivationPolicy.OnGranted)
                    _asc.TryActivateAbility(attackAbilityData);
            }

            // Grant the skill ability (optional — towers without a skill have this null).
            if (skillAbilityData != null)
            {
                var spec = _asc.GiveAbility(skillAbilityData);
                spec.cooldownRateAttr = EGameplayAttributeType.SkillCooldownRate;

                // Auto-activate if policy is OnGranted (e.g. passive/aura)
                if (skillAbilityData.activationPolicy == EAbilityActivationPolicy.OnGranted)
                    _asc.TryActivateAbility(skillAbilityData);
            }
            // ── Render ─────────────────────────────────────────────────────────
            renderService.RenderUnit(TowerID, InstanceID, Position);
            renderInitialized = true;

            // ── VFX ────────────────────────────────────────────────────────────
            vfxController = new StatusEffectVFXController(
                instanceID,
                () => Position,
                eventBus,
                vfxManager,
                vfxConfig
            );

            _eventBus = eventBus;

            // ── Unit Identity ──────────────────────────────────────────────────
            _asc.UnitInstanceID = instanceID;

            // ── Hit Notification Subscription ──────────────────────────────────
            eventBus.Subscribe<GameplayEffectAppliedEvent>(HandleEffectApplied);
        }

        /// <summary>Called every frame by TowerManager.Tick().</summary>
        public void Tick(float dt)
        {
            // Tick the GAS to update cooldowns and active effects
            _asc.Tick();

            // Skip if disabled / silenced
            if (_asc.HasAnyTags(GameplayTag.State_Disabled, GameplayTag.State_Silenced))
                return;

            TryFireAtEnemy(dt);
            TryUseSkill();

            vfxController?.Tick(dt);
        }

        /// <summary>Remove render layer and proxy Transform. Called by TowerManager.</summary>
        public void Cleanup()
        {
            _attributeSet.Health.OnValueChanged -= HandleHealthValueChanged;

            if (renderInitialized)
            {
                renderService.RemoveRender(TowerID, InstanceID);
                renderInitialized = false;
            }

            if (proxyGO != null)
            {
                UnityEngine.Object.Destroy(proxyGO);
                proxyGO = null;
            }

            vfxController?.Dispose();

            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<GameplayEffectAppliedEvent>(HandleEffectApplied);
            }
        }

        // ── Private ──────────────────────────────────────────────────────────────

        private void TryFireAtEnemy(float dt)
        {
            if (attackAbilityData == null) return;
            if (attackAbilityData.activationPolicy == EAbilityActivationPolicy.OnGranted) return;

            // Tick down the ROF timer.
            // Scale the decrement by the NormalCooldownRate attribute so attack speed
            // buffs/debuffs that affect cooldown rate also affect the tower's firing interval.
            float rate = _attributeSet.NormalCooldownRate.CurrentValue;
            if (rate > 0.0001f)
            {
                _attackTimer -= dt / rate;
            }

            if (_attackTimer > 0f) return;

            // TryActivateAbility internally calls IAbilityBehaviour.CanActivate first.
            // The behaviour will query IEnemyManager for the closest target.
            bool activated = _asc.TryActivateAbility(attackAbilityData);
            if (activated)
            {
                // Compute next base interval from the live ROF attribute.
                // Note: The 'rate' multiplier is applied during Tick above.
                float rof = _attributeSet.ROF.CurrentValue;
                _attackTimer = rof > 0f ? 1.0f / rof : float.MaxValue;

                TriggerProcs(EProcTriggerCondition.OnAttackStart);
                TriggerProcs(EProcTriggerCondition.EveryNthAttack);
            }
        }

        private void TryUseSkill()
        {
            if (skillAbilityData == null) return;
            if (skillAbilityData.activationPolicy == EAbilityActivationPolicy.OnGranted) return;
            if (_asc.IsAbilityOnCooldown(skillAbilityData)) return;

            // GAS cooldown on the skill SO governs the fire rate — no extra timer needed.
            _asc.TryActivateAbility(skillAbilityData);
        }

        private void HandleHealthValueChanged(float oldValue, float newValue)
        {
            // Event-driven: called only when HP changes, never every frame.
            if (renderInitialized)
                renderService.SetHpPercent(TowerID, InstanceID, newValue * maxHealthInverse);
        }

        private void HandleEffectApplied(GameplayEffectAppliedEvent evt)
        {
            // Identity Check: Only trigger if the effect was applied by OUR normal attack ability.
            if (evt.SourceInstanceID == InstanceID && evt.SourceAbility == attackAbilityData)
            {
                OnEnemyHit(evt.TargetASC);
            }
        }

        // ── Procs ────────────────────────────────────────────────────────────────

        public void OnEnemyHit(AbilitySystemComponent targetAsc)
        {
            TriggerProcs(EProcTriggerCondition.OnHit, targetAsc);
        }

        public void OnEnemyKilled(AbilitySystemComponent targetAsc)
        {
            TriggerProcs(EProcTriggerCondition.OnKill, targetAsc);
        }

        private void TriggerProcs(EProcTriggerCondition condition, AbilitySystemComponent targetAsc = null)
        {
            // Optimization: Use a standard for-loop instead of foreach.
            // 1. Avoids heap allocation of a new List or Enumerator on every hit.
            // 2. Avoids InvalidOperationException because index-based access doesn't check for collection modification.
            // 3. New abilities granted during iteration (implicit grant) are safely appended and can be processed or skipped in the same frame.
            var abilities = _asc.GrantedAbilities;
            for (int i = 0; i < abilities.Count; i++)
            {
                var ability = abilities[i];
                if (ability is TD_BaseProcData procData)
                {
                    if (_behaviourRegistry.GetBehaviour(procData) is TD_BaseProcBehaviour behaviour)
                    {
                        behaviour.EvaluateProc(procData, _asc, condition, targetAsc);
                    }
                }
            }
        }
    }
}
