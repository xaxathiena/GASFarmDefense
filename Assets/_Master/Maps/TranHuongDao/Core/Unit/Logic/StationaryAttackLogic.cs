using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Logic for units that stay in one place and attack enemies (Standard Tower logic).
    /// </summary>
    public class StationaryAttackLogic : IUnitLogic
    {
        private readonly IEnemyManager _enemyManager;

        public StationaryAttackLogic(IEnemyManager enemyManager)
        {
            _enemyManager = enemyManager;
        }

        public void OnEnter(Minion minion) { }

        public void Tick(Minion minion, float dt)
        {
            // Note: In a fully refactored system, Tower would use this.
            // For now, this is available for stationary summons (e.g. Sentry/Palisade).
            
            // Logic: Just wait for GAS to trigger the Attack Ability (which handles its own cooldown/range)
            // or we can manually call TryActivateAbility here if we wanted non-GAS autonomous logic.
            
            // For a "Minion", let's make it try to attack the closest enemy automatically.
            if (minion.AttackAbility != null)
            {
                minion.ASC.TryActivateAbility(minion.AttackAbility);
            }
        }

        public void OnExit(Minion minion) { }
    }
}
