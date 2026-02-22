using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Minimal attribute set for a TD tower.
    /// Towers are typically indestructible in this game, so only Attack-related attributes matter.
    /// </summary>
    public class TowerAttributeSet : AttributeSet
    {
        public readonly GameplayAttribute Health    = new GameplayAttribute(500f);
        public readonly GameplayAttribute MaxHealth = new GameplayAttribute(500f);
        public readonly GameplayAttribute Damage    = new GameplayAttribute(20f);
        public readonly GameplayAttribute AttackRange = new GameplayAttribute(8f);

        public TowerAttributeSet()
        {
            RegisterAttribute(nameof(Health),      Health);
            RegisterAttribute(nameof(MaxHealth),   MaxHealth);
            RegisterAttribute(nameof(Damage),      Damage);
            RegisterAttribute(nameof(AttackRange), AttackRange);
        }

        public bool IsAlive => Health.CurrentValue > 0f;

        public void FullRestore()
        {
            Health.SetCurrentValue(MaxHealth.CurrentValue);
        }
    }
}
