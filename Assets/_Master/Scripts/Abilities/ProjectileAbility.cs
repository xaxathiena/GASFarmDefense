using System.Collections.Generic;
using GAS;
using FD.Character;
using FD.Projectiles;
using UnityEngine;

namespace FD.Ability
{
    public enum ProjectileMovementType
    {
        Linear,
        Parabolic,
        Homing
    }

    [CreateAssetMenu(fileName = "ProjectileAbility", menuName = "GAS/Abilities/Projectile Ability")]
    public class ProjectileAbility : GameplayAbility
    {
        [Header("Projectile")]
        public GameObject projectilePrefab;
        public GameplayEffect gameplayEffect;
        public float speed = 10f;
        public ProjectileMovementType movementType = ProjectileMovementType.Linear;
        public float arcHeight = 2f;
        public float hitRadius = 0.25f;
        public float lifeTime = 5f;

        [Header("VFX")]
        public GameObject muzzleFlashVfx;
        public GameObject impactVfx;
        public Transform muzzleTransform;

        [Header("Audio")]
        public AudioClip onSpawn;
        public AudioClip onTravel;
        public AudioClip onHit;

        protected override void OnAbilityActivated(AbilitySystemComponent asc, GameplayAbilitySpec spec)
        {
            var abilityOwner = GetAbilityOwner(asc);
            if (abilityOwner == null || projectilePrefab == null)
            {
                EndAbility(asc);
                return;
            }

            var character = abilityOwner.GetComponent<BaseCharacter>();
            if (character == null)
            {
                EndAbility(asc);
                return;
            }

            List<Transform> targets = character.GetTargets();
            if (targets == null || targets.Count == 0)
            {
                EndAbility(asc);
                return;
            }

            Transform firePoint = muzzleTransform != null ? muzzleTransform : abilityOwner.transform;

            foreach (var target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                if (muzzleFlashVfx != null)
                {
                    Object.Instantiate(muzzleFlashVfx, firePoint.position, firePoint.rotation);
                }

                if (onSpawn != null)
                {
                    AudioSource.PlayClipAtPoint(onSpawn, firePoint.position);
                }

                var projectileObject = Object.Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
                var projectile = projectileObject.GetComponent<ProjectileBase>();
                if (projectile == null)
                {
                    projectile = projectileObject.AddComponent<ProjectileBase>();
                }

                projectile.Initialize(target, speed, movementType, arcHeight, hitRadius, lifeTime, impactVfx, onTravel, onHit, asc, gameplayEffect);
            }

            EndAbility(asc);
        }
    }
}
