using System;
using UnityEngine;
using GAS;

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
    public class Tower
    {
        // ── Identity ─────────────────────────────────────────────────────────────
        public int    InstanceID { get; private set; }
        public string TowerID   { get; private set; }
        public Vector3 Position { get; private set; }

        // ── GAS ──────────────────────────────────────────────────────────────────
        /// <summary>Exposed so external systems can apply effects (e.g. slow, buff).</summary>
        public AbilitySystemComponent ASC => asc;

        private readonly AbilitySystemComponent asc;
        private readonly TowerAttributeSet      attributeSet = new TowerAttributeSet();

        // The AbilityData asset that represents our NormalAttack.
        // Injected from TowerManager so it can be a ScriptableObject reference.
        private TDTowerNormalAttackData normalAttackData;

        // ── Render ───────────────────────────────────────────────────────────────
        private IRender2DService renderService;
        private bool             renderInitialized;

        // ── Proxy Transform (needed by ASC.GetOwner() for ability range checks) ──
        // A lightweight GameObject created once and destroyed on Cleanup().
        private GameObject proxyGO;

        // ── Events ───────────────────────────────────────────────────────────────
        public event Action<Tower> OnDestroyed;

        // ── Attack throttle ──────────────────────────────────────────────────────
        // TryActivateAbility is guarded by GAS cooldown, but we poll this every frame.
        // No extra throttle needed; the cooldown defined in TDTowerNormalAttackData drives the rate.

        // ─────────────────────────────────────────────────────────────────────────
        public Tower(AbilitySystemComponent asc)
        {
            this.asc = asc;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        public void Initialize(
            int                      instanceID,
            string                   towerID,
            Vector3                  position,
            float                    maxHealth,
            TDTowerNormalAttackData  normalAttackData,
            IRender2DService         renderService)
        {
            InstanceID  = instanceID;
            TowerID     = towerID;
            Position    = position;
            this.normalAttackData = normalAttackData;
            this.renderService    = renderService;

            // ── Proxy Transform ────────────────────────────────────────────────
            // The ASC needs a Transform so ability behaviours can read owner position.
            proxyGO                   = new GameObject($"TowerProxy_{instanceID}");
            proxyGO.transform.position = position;
            asc.InitOwner(proxyGO.transform);

            // ── GAS setup ──────────────────────────────────────────────────────
            attributeSet.MaxHealth.SetBaseValue(maxHealth);
            attributeSet.Health.SetBaseValue(maxHealth);
            attributeSet.FullRestore();
            asc.InitializeAttributeSet(attributeSet);

            // Grant the NormalAttack ability so the ASC can activate it
            if (normalAttackData != null)
                asc.GiveAbility(normalAttackData);

            // ── Render ─────────────────────────────────────────────────────────
            renderService.RenderUnit(TowerID, InstanceID, Position);
            renderInitialized = true;
        }

        /// <summary>Called every frame by TowerManager.Tick().</summary>
        public void Tick(float dt)
        {
            // Tick the GAS to update cooldowns and active effects
            asc.Tick();

            // Skip if disabled / silenced
            if (asc.HasAnyTags(GameplayTag.State_Disabled, GameplayTag.State_Silenced))
                return;

            TryFireAtEnemy();
        }

        /// <summary>Remove render layer and proxy Transform. Called by TowerManager.</summary>
        public void Cleanup()
        {
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
        }

        // ── Private ──────────────────────────────────────────────────────────────

        private void TryFireAtEnemy()
        {
            if (normalAttackData == null) return;

            // TryActivateAbility internally calls IAbilityBehaviour.CanActivate first.
            // The TowerAttackAbilityBehaviour will query IEnemyManager for closest target.
            asc.TryActivateAbility(normalAttackData);
        }
    }
}
