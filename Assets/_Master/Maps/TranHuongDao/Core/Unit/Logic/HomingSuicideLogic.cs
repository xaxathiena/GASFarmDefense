using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Logic for units that move towards the closest enemy and self-destruct.
    /// Used by the Goblin Bomber.
    /// </summary>
    public class HomingSuicideLogic : IUnitLogic
    {
        private readonly IEnemyManager _enemyManager;

        public HomingSuicideLogic(IEnemyManager enemyManager)
        {
            _enemyManager = enemyManager;
        }

        public void OnEnter(Minion minion) { }

        public void Tick(Minion minion, float dt)
        {
            // 1. Find target
            int targetID = _enemyManager.GetClosestEnemyInRange(minion.Position, 50f); // Large search radius
            if (targetID == -1) return;

            if (_enemyManager.TryGetEnemyPosition(targetID, out Vector3 targetPos))
            {
                // 2. Move towards target
                Vector3 direction = (targetPos - minion.Position);
                float distance = direction.magnitude;

                if (distance > 0.5f)
                {
                    Vector3 move = direction.normalized * minion.Config.MoveSpeed * dt;
                    minion.Position += move;
                    minion.Rotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                }
                else
                {
                    // 3. Explode!
                    Explode(minion, targetID);
                }
            }
        }

        private void Explode(Minion minion, int targetID)
        {
            // Trigger explosion ability if any
            if (minion.AttackAbility != null)
            {
                if (minion.ASC.GetAbilitySpec(minion.AttackAbility) == null)
                {
                    minion.ASC.GiveAbility(minion.AttackAbility);
                }

                bool success = minion.ASC.TryActivateAbility(minion.AttackAbility);
                if (!success) Debug.LogWarning($"[HomingSuicideLogic] Failed to activate ability {minion.AttackAbility?.name}");
            }
            else
            {
                Debug.LogWarning($"[HomingSuicideLogic] No AttackAbility instance for {minion.UnitID}");
            }

            // Self-destruct
            minion.Destroy();
        }

        public void OnExit(Minion minion) { }
    }
}
