using System;
using System.Collections.Generic;
using UnityEngine;
using GAS;
using Abel.TranHuongDao.Core.VFX;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Pure C# enemy entity (no MonoBehaviour – rendered via the Render2D batch system).
    ///
    /// Lifecycle:
    ///   1. new Enemy(asc)       – created by EnemyManager via IObjectResolver
    ///   2. Initialize(...)      – configures stats, path, render link
    ///   3. Tick(dt)             – called every frame by EnemyManager (ITickable)
    ///   4. OnDeath fires        – EnemyManager cleans up and calls RemoveRender
    /// </summary>
    public class Enemy
    {
        // ── Identity ─────────────────────────────────────────────────────────────
        public int InstanceID { get; private set; }
        public string EnemyID { get; private set; }

        // ── Transform ────────────────────────────────────────────────────────────
        public Vector3 Position { get; private set; }
        public float Rotation { get; private set; }   // degrees, Y-axis

        // ── State ────────────────────────────────────────────────────────────────
        public bool IsAlive => attributeSet.IsAlive;
        public bool HasReachedEnd { get; private set; }

        // ── GAS ──────────────────────────────────────────────────────────────────
        /// <summary>Exposed so towers / abilities can apply GameplayEffects to this enemy.</summary>
        public AbilitySystemComponent ASC => asc;

        private readonly AbilitySystemComponent asc;
        private readonly UnitAttributeSet attributeSet = new UnitAttributeSet();

        // ── Render ───────────────────────────────────────────────────────────────
        private IRender2DService renderService;
        private bool renderInitialized;

        // ── VFX ──────────────────────────────────────────────────────────────────
        private StatusEffectVFXController vfxController;

        // Cached max-health inverse: avoids a division every time HP changes.
        private float maxHealthInverse;

        // ── Path following ───────────────────────────────────────────────────────
        private IReadOnlyList<Vector3> path;
        private int waypointIndex;
        private float moveSpeed;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>
        /// Fired when health reaches 0.
        /// Arg: this enemy instance.  EnemyManager subscribes to this.
        /// </summary>
        public event Action<Enemy> OnDeath;

        /// <summary>
        /// Fired when the enemy walks off the last waypoint (reached player base).
        /// Arg: this enemy instance.
        /// </summary>
        public event Action<Enemy> OnReachedEnd;

        // ─────────────────────────────────────────────────────────────────────────
        public Enemy(AbilitySystemComponent asc)
        {
            this.asc = asc;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Fully configure this enemy before the first Tick.
        /// Called by EnemyManager right after construction.
        /// </summary>
        public void Initialize(
            int instanceID,
            string enemyID,
            UnitConfig config,
            IReadOnlyList<Vector3> path,
            IRender2DService renderService,
            FD.IEventBus eventBus,
            FD.Modules.VFX.IVFXManager vfxManager,
            TagVFXConfig vfxConfig)
        {
            InstanceID = instanceID;
            EnemyID = enemyID;

            this.path = path;
            this.moveSpeed = config.MoveSpeed;   // cached for per-frame use in MoveAlongPath
            this.renderService = renderService;

            waypointIndex = 0;
            HasReachedEnd = false;
            Position = path.Count > 0 ? path[0] : Vector3.zero;
            Rotation = 0f;

            // ── GAS setup ──────────────────────────────────────────────────────
            // Seed all GAS attributes from the authored balance config.
            attributeSet.InitializeFromConfig(config);
            asc.InitializeAttributeSet(attributeSet);

            // Cache the reciprocal once so HP changes only cost a multiply, not a divide.
            maxHealthInverse = config.MaxHealth > 0f ? 1f / config.MaxHealth : 1f;

            // Subscribe AFTER InitializeAttributeSet so the ASC owner pointer is valid.
            attributeSet.OnHealthDepleted += HandleHealthDepleted;
            attributeSet.Health.OnValueChanged += HandleHealthValueChanged;

            // ── Render ─────────────────────────────────────────────────────────
            renderService.RenderUnit(EnemyID, InstanceID, Position, Rotation);
            renderInitialized = true;

            // ── VFX ────────────────────────────────────────────────────────────
            vfxController = new StatusEffectVFXController(
                instanceID,
                () => Position,
                eventBus,
                vfxManager,
                vfxConfig
            );
        }

        /// <summary>
        /// Called every frame by EnemyManager.Tick().
        /// </summary>
        public void Tick(float dt)
        {
            if (!IsAlive || HasReachedEnd) return;

            // Tick the GAS (updates cooldowns, active effects like burning damage)
            asc.Tick();

            // Move along the path
            MoveAlongPath(dt);

            // Push new position to the Render2D pipeline
            if (renderInitialized)
                renderService.UpdateRender(EnemyID, InstanceID, Position, Rotation);

            vfxController?.Tick(dt);
        }

        /// <summary>
        /// Force-remove the render layer (called by EnemyManager on death/despawn
        /// before this enemy is discarded).
        /// </summary>
        public void Cleanup()
        {
            attributeSet.OnHealthDepleted -= HandleHealthDepleted;
            attributeSet.Health.OnValueChanged -= HandleHealthValueChanged;

            if (renderInitialized)
            {
                renderService.RemoveRender(EnemyID, InstanceID);
                renderInitialized = false;
            }

            vfxController?.Dispose();
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private void MoveAlongPath(float dt)
        {
            if (path == null || waypointIndex >= path.Count) return;

            Vector3 target = path[waypointIndex];
            Vector3 direction = (target - Position);
            float distance = direction.magnitude;
            float step = moveSpeed * dt;

            if (step >= distance)
            {
                // Arrived at this waypoint – snap and advance
                Position = target;
                waypointIndex++;

                if (waypointIndex >= path.Count)
                {
                    HasReachedEnd = true;
                    OnReachedEnd?.Invoke(this);
                    return;
                }
            }
            else
            {
                // Move toward waypoint
                Vector3 move = direction.normalized * step;
                Position = Position + move;

                // Store facing angle (XZ plane, degrees)
                Rotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            }
        }

        private void HandleHealthValueChanged(float oldValue, float newValue)
        {
            // Event-driven: called only when HP changes, never every frame.
            if (renderInitialized)
                renderService.SetHpPercent(EnemyID, InstanceID, newValue * maxHealthInverse);
        }

        private void HandleHealthDepleted()
        {
            // Unsubscribe immediately so this fires only once.
            attributeSet.OnHealthDepleted -= HandleHealthDepleted;
            OnDeath?.Invoke(this);
        }
    }
}
