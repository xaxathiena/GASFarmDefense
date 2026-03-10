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
        private const string DefaultRenderUnitID = "bullet_normal";

        // ── Static ID counter ────────────────────────────────────────────────────
        private static int _idCounter;

        // ── Identity ─────────────────────────────────────────────────────────────
        public readonly int InstanceID;
        public readonly string TrailID;
        private readonly string _trailVfxID;
        private readonly string _hitVfxID;
        private readonly int _trailVfxHandleID; // Holds the ID if Effekseer handles it

        /// <summary>True until this bullet hits a target or the target disappears.</summary>
        public bool IsAlive { get; private set; } = true;

        // ── Runtime dependencies ─────────────────────────────────────────────────
        private readonly int _targetEnemyID;
        private readonly AbilitySystemComponent _sourceASC;
        private readonly GameplayEffect _damageEffect;
        private readonly float _damageAmount;
        private readonly float _speed;
        private readonly float _collisionSqr; // collisionThreshold²

        private readonly IEnemyManager _enemyManager;
        private readonly IRender2DService _renderService;
        private readonly FD.Modules.VFX.IVFXManager _vfxManager;
        private readonly System.Action<Vector3, int> _onHitCallback;

        // ── State ────────────────────────────────────────────────────────────────
        private Vector3 _position;

        // ── Constructor ──────────────────────────────────────────────────────────

        public Bullet(
            string trailID,
            string trailVfxID,
            string hitVfxID,
            int targetEnemyInstanceID,
            Vector3 spawnPosition,
            AbilitySystemComponent sourceASC,
            GameplayEffect damageEffect,
            float damageAmount,
            float bulletSpeed,
            float collisionThreshold,
            IEnemyManager enemyManager,
            IRender2DService renderService,
            FD.Modules.VFX.IVFXManager vfxManager,
            System.Action<Vector3, int> onHit = null)
        {
            InstanceID = ++_idCounter;

            TrailID = string.IsNullOrEmpty(trailID) ? DefaultRenderUnitID : trailID;
            _trailVfxID = trailVfxID;
            _hitVfxID = hitVfxID;
            _targetEnemyID = targetEnemyInstanceID;
            _position = spawnPosition;
            _sourceASC = sourceASC;
            _damageEffect = damageEffect;
            _damageAmount = damageAmount;
            _speed = bulletSpeed;
            _collisionSqr = collisionThreshold * collisionThreshold;
            _enemyManager = enemyManager;
            _renderService = renderService;
            _vfxManager = vfxManager;
            _onHitCallback = onHit;

            // Register with the render pipeline
            if (string.IsNullOrEmpty(_trailVfxID))
            {
                // Only render 2D if no Effekseer trail is provided
                _renderService.RenderUnit(TrailID, InstanceID, _position);
            }
            else
            {
                // Play Effekseer VFX for bullet trail
                _trailVfxHandleID = _vfxManager.PlayEffectAt(_trailVfxID, _position);
            }
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

            // ── Update renderer / VFX ────────────────────────────────────────────
            if (string.IsNullOrEmpty(_trailVfxID))
            {
                _renderService.UpdateRender(TrailID, InstanceID, _position);
            }
            else if (_trailVfxHandleID > 0)
            {
                _vfxManager.UpdateEffectPosition(_trailVfxHandleID, _position);
            }
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
            // ── Fallback: direct damage via UnitAttributeSet ─────────────────────
            else if (_enemyManager.TryGetEnemyASC(_targetEnemyID, out AbilitySystemComponent fallbackASC))
            {
                var attrSet = fallbackASC.GetAttributeSet<UnitAttributeSet>();
                if (attrSet != null)
                {
                    attrSet.TakeDamage(_damageAmount);
                    Debug.Log($"[Bullet] Direct damage {_damageAmount} → enemy {_targetEnemyID}");
                }
            }

            // ── Trigger Hit VFX ───────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(_hitVfxID))
            {
                _vfxManager.PlayEffectAt(_hitVfxID, _position);
            }

            _onHitCallback?.Invoke(_position, _targetEnemyID);

            Destroy();
        }

        private void Destroy()
        {
            if (!IsAlive) return;
            IsAlive = false;

            if (string.IsNullOrEmpty(_trailVfxID))
            {
                _renderService.RemoveRender(TrailID, InstanceID);
            }
            else if (_trailVfxHandleID > 0)
            {
                _vfxManager.StopEffect(_trailVfxHandleID);
            }
        }
    }
}
