using FD.Ability;
using FD.Core;
using GAS;
using UnityEngine;

namespace FD.Projectiles
{
    public class ProjectileBase : MonoBehaviour
    {
        private Transform target;
        private float speed;
        private ProjectileMovementType movementType;
        private float arcHeight;
        private float hitRadius;
        private float lifeTime;
        private GameObject impactVfx;
        private AudioClip onTravel;
        private AudioClip onHit;

        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float elapsed;
        private float travelTime;
        private AudioSource travelSource;
        private GameplayEffect gameplayEffect;
        private AbilitySystemComponent sourceASC;
        private GameplayAbilitySpec spec;
        private float effectLevel = 1f;
        private FDGameplayAbility owningAbility;

        public void Initialize(
            Transform target,
            float speed,
            ProjectileMovementType movementType,
            float arcHeight,
            float hitRadius,
            float lifeTime,
            GameObject impactVfx,
            AudioClip onTravel,
            AudioClip onHit,
            AbilitySystemComponent sourceASC,
            GameplayEffect gameplayEffect,
            GameplayAbilitySpec spec,
            float effectLevel,
            FDGameplayAbility owningAbility)
        {
            this.target = target;
            this.speed = Mathf.Max(0.01f, speed);
            this.movementType = movementType;
            this.arcHeight = Mathf.Max(0f, arcHeight);
            this.hitRadius = Mathf.Max(0.01f, hitRadius);
            this.lifeTime = Mathf.Max(0.1f, lifeTime);
            this.impactVfx = impactVfx;
            this.onTravel = onTravel;
            this.onHit = onHit;
            this.gameplayEffect = gameplayEffect;
            this.sourceASC = sourceASC;
            this.spec = spec;
            this.effectLevel = Mathf.Max(1f, effectLevel);
            this.owningAbility = owningAbility;
            startPosition = transform.position;
            targetPosition = target != null ? target.position : startPosition + transform.forward * 5f;

            float distance = Vector3.Distance(startPosition, targetPosition);
            travelTime = distance / this.speed;

            ConfigureTravelAudio(onTravel);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= lifeTime)
            {
                ReturnToPool();
                return;
            }

            switch (movementType)
            {
                case ProjectileMovementType.Parabolic:
                    UpdateParabolic();
                    break;
                case ProjectileMovementType.Homing:
                    UpdateHoming();
                    break;
                default:
                    UpdateLinear();
                    break;
            }
        }

        private void UpdateLinear()
        {
            Vector3 destination = target != null ? targetPosition : targetPosition;
            MoveTowards(destination);
        }

        private void UpdateHoming()
        {
            Vector3 destination = target != null ? target.position : targetPosition;
            MoveTowards(destination);
        }

        private void UpdateParabolic()
        {
            if (travelTime <= 0f)
            {
                Hit();
                return;
            }

            float t = Mathf.Clamp01(elapsed / travelTime);
            Vector3 pos = Vector3.Lerp(startPosition, targetPosition, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = pos;

            if (t >= 1f)
            {
                Hit();
            }
        }

        private void MoveTowards(Vector3 destination)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, destination) <= hitRadius)
            {
                Hit();
            }
        }

        private void Hit()
        {
            if (impactVfx != null)
            {
                Instantiate(impactVfx, transform.position, Quaternion.identity);
            }

            if (onHit != null)
            {
                AudioSource.PlayClipAtPoint(onHit, transform.position);
            }

            if (travelSource != null)
            {
                travelSource.Stop();
            }
            if (gameplayEffect != null && target != null)
            {
                // get the AbilitySystemComponent of the target from IAbilitySystemComponent
                var targetAsc = target.GetAbilitySystemComponent();
                if (targetAsc != null)
                {
                    if (owningAbility != null)
                    {
                        owningAbility.ApplyEffectToTarget(gameplayEffect, sourceASC, targetAsc, spec);
                    }
                    else
                    {
                        targetAsc.ApplyGameplayEffectToSelf(gameplayEffect, sourceASC, effectLevel);
                    }
                }
            }
            ReturnToPool();
        }

        private void ConfigureTravelAudio(AudioClip clip)
        {
            if (clip == null)
            {
                if (travelSource != null)
                {
                    travelSource.Stop();
                    travelSource.clip = null;
                }
                return;
            }

            if (travelSource == null)
            {
                travelSource = gameObject.AddComponent<AudioSource>();
            }

            travelSource.clip = clip;
            travelSource.loop = true;
            travelSource.playOnAwake = false;
            travelSource.spatialBlend = 1f;
            travelSource.Play();
        }

        private void ReturnToPool()
        {
            if (travelSource != null)
            {
                travelSource.Stop();
                travelSource.clip = null;
            }

            elapsed = 0f;
            target = null;
            //oolManager.Despawn(this);
        }
    }
}
