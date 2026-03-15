using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// IAbilityBehaviour implementation for <see cref="TDTowerNormalAttackData"/>.
    /// Registered as a Singleton in VContainer and mapped explicitly by
    /// <see cref="TDGASInitializer"/>.
    ///
    /// Flow each time TryActivateAbility is called by Tower:
    ///   CanActivate → verify at least one enemy is within range
    ///   OnActivated → spawn a <see cref="Bullet"/> via <see cref="IBulletManager"/>
    ///
    /// Attack rate is controlled by Tower's internal ROF timer — this behaviour
    /// does NOT set any GAS cooldown, so the GAS cooldown is free for skills.
    /// </summary>
    public class TDTowerNormalAttackBehaviour : IAbilityBehaviour
    {
        private readonly IEnemyManager _enemyManager;
        private readonly IBulletManager _bulletManager;

        public TDTowerNormalAttackBehaviour(
            IEnemyManager enemyManager,
            IBulletManager bulletManager)
        {
            _enemyManager = enemyManager;
            _bulletManager = bulletManager;
        }

        // ── IAbilityBehaviour ────────────────────────────────────────────────────

        public bool CanActivate(
            GameplayAbilityData data,
            AbilitySystemComponent asc,
            GameplayAbilitySpec spec)
        {
            if (data is not TDTowerNormalAttackData) return false;

            // Read acquisition radius directly from the owner's live attribute —
            // this automatically respects any range-buff or range-debuff applied via GAS.
            var unitAttr = asc.GetAttributeSet<UnitAttributeSet>();
            if (unitAttr == null) return false;

            Vector3 origin = GetOwnerPosition(asc);
            return _enemyManager.GetClosestEnemyInRange(origin, unitAttr.AttackRange.CurrentValue) != -1;
        }

        public void OnActivated(
            GameplayAbilityData data,
            AbilitySystemComponent asc,
            GameplayAbilitySpec spec)
        {
            var attackData = data as TDTowerNormalAttackData;
            if (attackData == null) return;

            // All numeric stats come from the owner's UnitAttributeSet so they
            // reflect buffs/debuffs applied through GAS at the exact moment of firing.
            var unitAttr = asc.GetAttributeSet<UnitAttributeSet>();
            if (unitAttr == null) return;

            float range = unitAttr.AttackRange.CurrentValue;
            float damage = unitAttr.Damage.CurrentValue;
            float projSpeed = unitAttr.ProjectileSpeed.CurrentValue;

            Vector3 spawnPos = GetOwnerPosition(asc);
            int targetID = _enemyManager.GetClosestEnemyInRange(spawnPos, range);
            if (targetID == -1) return;

            // ── Spawn a bullet that will travel to the target ─────────────────────
            _bulletManager.SpawnBullet(
                trailID: attackData.trailID,
                trailVfxID: attackData.trailVfxID,
                hitVfxID: attackData.hitVfxID,
                targetEnemyInstanceID: targetID,
                spawnPosition: spawnPos,
                sourceASC: asc,
                damageEffect: attackData.damageEffect,
                damageAmount: damage,
                bulletSpeed: projSpeed,
                collisionThreshold: attackData.collisionThreshold,
                sourceAbility: attackData);

            Debug.Log($"[TowerAttack] Bullet fired at enemy {targetID}");
        }

        public void OnEnded(GameplayAbilityData _, AbilitySystemComponent __, GameplayAbilitySpec ___) { }
        public void OnCancelled(GameplayAbilityData _, AbilitySystemComponent __, GameplayAbilitySpec ___) { }

        // ── Private ──────────────────────────────────────────────────────────────

        private static Vector3 GetOwnerPosition(AbilitySystemComponent asc)
        {
            var owner = asc.GetOwner();
            return owner != null ? owner.position : Vector3.zero;
        }
    }
}
