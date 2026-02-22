using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Owns the lifecycle of every active <see cref="Bullet"/> in the scene.
    ///
    /// Registered in VContainer as a Singleton entry-point:
    ///   • <see cref="IBulletManager"/> — public API for spawning bullets
    ///   • <see cref="ITickable"/>      — drives bullet movement each frame
    ///   • <see cref="IDisposable"/>    — cleans up on scope disposal
    /// </summary>
    public sealed class BulletManager : IBulletManager, ITickable, IDisposable
    {
        // ── Dependencies ─────────────────────────────────────────────────────────
        private readonly IEnemyManager    _enemyManager;
        private readonly IRender2DService _renderService;

        // ── State ────────────────────────────────────────────────────────────────
        private readonly List<Bullet> _activeBullets  = new List<Bullet>(64);
        private readonly List<Bullet> _removalBuffer  = new List<Bullet>(16);

        // ── Constructor ──────────────────────────────────────────────────────────

        public BulletManager(IEnemyManager enemyManager, IRender2DService renderService)
        {
            _enemyManager  = enemyManager;
            _renderService = renderService;
        }

        // ── IBulletManager ───────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void SpawnBullet(
            int              targetEnemyInstanceID,
            Vector3          spawnPosition,
            AbilitySystemComponent sourceASC,
            GameplayEffect   damageEffect,
            float            damageAmount,
            float            bulletSpeed,
            float            collisionThreshold)
        {
            var bullet = new Bullet(
                targetEnemyInstanceID,
                spawnPosition,
                sourceASC,
                damageEffect,
                damageAmount,
                bulletSpeed,
                collisionThreshold,
                _enemyManager,
                _renderService);

            _activeBullets.Add(bullet);
            Debug.Log($"[BulletManager] Spawned bullet #{bullet.InstanceID} → enemy {targetEnemyInstanceID}");
        }

        // ── ITickable ─────────────────────────────────────────────────────────────

        public void Tick()
        {
            float dt = Time.deltaTime;

            // Advance all bullets
            for (int i = 0; i < _activeBullets.Count; i++)
            {
                _activeBullets[i].Tick(dt);
            }

            // Collect dead bullets to avoid modifying list during iteration
            for (int i = _activeBullets.Count - 1; i >= 0; i--)
            {
                if (!_activeBullets[i].IsAlive)
                {
                    _removalBuffer.Add(_activeBullets[i]);
                    _activeBullets.RemoveAt(i);
                }
            }

            _removalBuffer.Clear();
        }

        // ── IDisposable ───────────────────────────────────────────────────────────

        public void Dispose()
        {
            // Force-destroy any remaining bullets (e.g., scene unload)
            foreach (var bullet in _activeBullets)
            {
                bullet.Tick(0f); // Tick with 0 dt won't move; just ensure destruction via flag
            }

            _activeBullets.Clear();
            Debug.Log("[BulletManager] Disposed — all bullets removed.");
        }
    }
}
