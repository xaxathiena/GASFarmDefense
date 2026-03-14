using System;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Unified GAS attribute set shared by every unit (Towers and Enemies).
    /// Towers default to MoveSpeed = 0; Enemies may default to Damage = 0.
    /// All runtime default values are intentionally 0 — real values are
    /// injected via InitializeFromConfig() after construction.
    /// </summary>
    public class UnitAttributeSet : AttributeSet
    {
        // ── Attributes ───────────────────────────────────────────────────────────

        public readonly GameplayAttribute Health          = new GameplayAttribute(0f);
        public readonly GameplayAttribute MaxHealth       = new GameplayAttribute(0f);
        public readonly GameplayAttribute MoveSpeed       = new GameplayAttribute(0f);
        public readonly GameplayAttribute Damage          = new GameplayAttribute(0f);
        public readonly GameplayAttribute AttackRange     = new GameplayAttribute(0f);
        public readonly GameplayAttribute ROF             = new GameplayAttribute(1f);
        public readonly GameplayAttribute ProjectileSpeed   = new GameplayAttribute(0f);
        public readonly GameplayAttribute NormalCooldownRate = new GameplayAttribute(1f);
        public readonly GameplayAttribute SkillCooldownRate  = new GameplayAttribute(1f);

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired exactly once when Health transitions from &gt; 0 to ≤ 0.</summary>
        public event Action OnHealthDepleted;

        // ── Constructor ─────────────────────────────────────────────────────────

        public UnitAttributeSet()
        {
            RegisterAttribute(nameof(Health),          Health);
            RegisterAttribute(nameof(MaxHealth),       MaxHealth);
            RegisterAttribute(nameof(MoveSpeed),       MoveSpeed);
            RegisterAttribute(nameof(Damage),          Damage);
            RegisterAttribute(nameof(AttackRange),     AttackRange);
            RegisterAttribute(nameof(ROF),             ROF);
            RegisterAttribute(nameof(ProjectileSpeed),   ProjectileSpeed);
            RegisterAttribute(nameof(NormalCooldownRate), NormalCooldownRate);
            RegisterAttribute(nameof(SkillCooldownRate),  SkillCooldownRate);

            Health.OnValueChanged += HandleHealthChanged;
        }

        // ── Convenience properties ───────────────────────────────────────────────

        public bool IsAlive => Health.CurrentValue > 0f;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Seeds all GAS attributes from a balance config struct.
        /// Health is initialised to MaxHealth so the unit starts at full health.
        /// </summary>
        public void InitializeFromConfig(UnitConfig config)
        {
            // Set BaseValue first; CurrentValue follows for unmodified attributes.
            MaxHealth.BaseValue       = config.MaxHealth;
            MaxHealth.CurrentValue    = config.MaxHealth;

            // Start the unit at full health.
            Health.BaseValue          = config.MaxHealth;
            Health.CurrentValue       = config.MaxHealth;

            MoveSpeed.BaseValue       = config.MoveSpeed;
            MoveSpeed.CurrentValue    = config.MoveSpeed;

            Damage.BaseValue          = config.BaseDamage;
            Damage.CurrentValue       = config.BaseDamage;

            AttackRange.BaseValue     = config.AttackRange;
            AttackRange.CurrentValue  = config.AttackRange;

            ROF.BaseValue             = config.ROF;
            ROF.CurrentValue          = config.ROF;

            ProjectileSpeed.BaseValue    = config.ProjectileSpeed;
            ProjectileSpeed.CurrentValue = config.ProjectileSpeed;

            NormalCooldownRate.BaseValue = 1f;
            NormalCooldownRate.CurrentValue = 1f;

            SkillCooldownRate.BaseValue = 1f;
            SkillCooldownRate.CurrentValue = 1f;
        }

        /// <summary>Apply damage, clamping Health to a minimum of 0.</summary>
        public void TakeDamage(float damage)
        {
            float newHp = Health.CurrentValue - damage;
            Health.SetCurrentValue(newHp < 0f ? 0f : newHp);
        }

        /// <summary>Restore Health to its current MaxHealth value.</summary>
        public void FullRestore()
        {
            Health.SetCurrentValue(MaxHealth.CurrentValue);
        }

        // ── Internal ─────────────────────────────────────────────────────────────

        private void HandleHealthChanged(float oldValue, float newValue)
        {
            if (newValue <= 0f && oldValue > 0f)
            {
                // Tag the owner ASC so abilities and effects can query death state.
                ownerASC?.AddTags(GameplayTag.State_Dead);
                OnHealthDepleted?.Invoke();
            }
        }
    }
}
