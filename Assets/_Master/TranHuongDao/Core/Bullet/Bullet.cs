using UnityEngine;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// A pure C# bullet that travels toward a target enemy and applies damage on hit.
    /// No MonoBehaviour, no Physics — lifecycle is owned by <see cref="BulletManager"/>.
    ///
    /// Rendering is done via <see cref="IRender2DService"/> using the unit ID "bullet_normal".
    /// </summary>
    public sealed class Bullet
    {
        // ── Constants ────────────────────────────────────────────────────────────
        private const string RenderUnitID = "bullet_normal";

        // ── Static ID counter ────────────────────────────────────────────────────
        private static int _idCounter;

        // ── Identity ─────────────────────────────────────────────────────────────
        public readonly int InstanceID;

        /// <summary>True until this bullet hits a target or the target disappears.</summary>
        public bool IsAlive { get; private set; } = true;

        // ── Runtime dependencies ─────────────────────────────────────────────────
        private readonly int             _targetEnemyID;
        private readonly AbilitySystemComponent _sourceASC;
        private readonly GameplayEffect  _damageEffect;
        private readonly float           _damageAmount;
        private readonly float           _speed;
        private readonly float           _collisionSqr; // collisionThreshold²

        private readonly IEnemyManager   _enemyManager;
        private readonly IRender2DService _renderService;

        // ── State ────────────────────────────────────────────────────────────────
        private Vector3 _position;

        // ── Constructor ──────────────────────────────────────────────────────────

        public Bullet(
            int              targetEnemyInstanceID,
            Vector3          spawnPosition,
            AbilitySystemComponent sourceASC,
            GameplayEffect   damageEffect,
            float            damageAmount,
            float            bulletSpeed,
            float            collisionThreshold,
            IEnemyManager    enemyManager,
            IRender2DService renderService)
        {
            InstanceID     = ++_idCounter;

            _targetEnemyID  = targetEnemyInstanceID;
            _position       = spawnPosition;
            _sourceASC      = sourceASC;
            _damageEffect   = damageEffect;
            _damageAmount   = damageAmount;
            _speed          = bulletSpeed;
            _collisionSqr   = collisionThreshold * collisionThreshold;
            _enemyManager   = enemyManager;
            _renderService  = renderService;

            // Register with the render pipeline
            _renderService.RenderUnit(RenderUnitID, InstanceID, _position);
        }

        // ── Update ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Move toward the target; trigger hit logic when close enough.
        /// Called every frame by <see cref="BulletManager"/>.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsAlive) return;

            // ── Resolve target position ──────────────────────────────────────────
            if (!_enemyManager.TryGetEnemyPosition(_targetEnemyID, out Vector3 targetPos))
            {
                // Target disappeared (died or left the field) — destroy bullet silently
                Destroy();
                return;
            }

            // ── Move toward target ───────────────────────────────────────────────
            Vector3 delta = targetPos - _position;
            float sqrDist = delta.sqrMagnitude;

            float stepDist = _speed * deltaTime;

            if (sqrDist <= _collisionSqr || stepDist * stepDist >= sqrDist)
            {
                // Close enough → register as a hit
                _position = targetPos;
                OnHit();
                return;
            }

            _position += delta.normalized * stepDist;

            // ── Update renderer ──────────────────────────────────────────────────
            _renderService.UpdateRender(RenderUnitID, InstanceID, _position);
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private void OnHit()
        {
            // ── Apply damage via GameplayEffect if one is assigned ────────────────
            if (_damageEffect != null &&
                _enemyManager.TryGetEnemyASC(_targetEnemyID, out AbilitySystemComponent targetASC))
            {
                targetASC.ApplyGameplayEffectToSelf(_damageEffect, _sourceASC);
                Debug.Log($"[Bullet] GE applied to enemy {_targetEnemyID}");
            }
            // ── Fallback: direct damage via EnemyAttributeSet ────────────────────
            else if (_enemyManager.TryGetEnemyASC(_targetEnemyID, out AbilitySystemComponent fallbackASC))
            {
                var attrSet = fallbackASC.GetAttributeSet<EnemyAttributeSet>();
                if (attrSet != null)
                {
                    attrSet.TakeDamage(_damageAmount);
                    Debug.Log($"[Bullet] Direct damage {_damageAmount} → enemy {_targetEnemyID}");
                }
            }

            Destroy();
        }

        private void Destroy()
        {
            if (!IsAlive) return;
            IsAlive = false;
            _renderService.RemoveRender(RenderUnitID, InstanceID);
        }
    }
}
