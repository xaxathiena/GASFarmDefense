using System;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Minimal attribute set for a TD enemy.
    /// Health is the only attribute that matters for gameplay death detection.
    /// MoveSpeed is read by Enemy.Tick() to drive path movement.
    /// </summary>
    public class EnemyAttributeSet : AttributeSet
    {
        // ── Attributes ──────────────────────────────────────────────────────────
        public readonly GameplayAttribute Health    = new GameplayAttribute(100f);
        public readonly GameplayAttribute MaxHealth = new GameplayAttribute(100f);
        public readonly GameplayAttribute MoveSpeed = new GameplayAttribute(3f);

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired exactly once when Health transitions from > 0 to ≤ 0.</summary>
        public event Action OnHealthDepleted;

        public EnemyAttributeSet()
        {
            RegisterAttribute(nameof(Health),    Health);
            RegisterAttribute(nameof(MaxHealth), MaxHealth);
            RegisterAttribute(nameof(MoveSpeed), MoveSpeed);

            Health.OnValueChanged += HandleHealthChanged;
        }

        // ── Convenience ──────────────────────────────────────────────────────────
        public bool IsAlive => Health.CurrentValue > 0f;

        /// <summary>Apply damage to health, clamped at 0.</summary>
        public void TakeDamage(float damage)
        {
            float newHp = Health.CurrentValue - damage;
            Health.SetCurrentValue(newHp < 0f ? 0f : newHp);
        }

        public void FullRestore()
        {
            Health.SetCurrentValue(MaxHealth.CurrentValue);
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void HandleHealthChanged(float oldValue, float newValue)
        {
            if (newValue <= 0f && oldValue > 0f)
            {
                // Add the standard dead tag so abilities/effects can query it
                ownerASC?.AddTags(GameplayTag.State_Dead);
                OnHealthDepleted?.Invoke();
            }
        }
    }
}
