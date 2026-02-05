using GAS;
using UnityEngine;

namespace FD.Character
{
    public abstract class EnemyBase : BaseCharacter
    {
        [Header("Enemy Specific")]
        [SerializeField] protected float detectionRange = 10f;
        [SerializeField] protected float attackRange = 2f;
        [SerializeField] protected Transform target;
        [Header("UI References")]
        [SerializeField] private DamagePopupManager damagePopupManager;

        protected override void Awake()
        {
            base.Awake();
            ResolveDamagePopupManager();
            // Enemy specific initialization
        }
        protected override void InitializeAttributeSet()
        {
            attributeSet.MoveSpeed.BaseValue = 3f;
            attributeSet.MaxHealth.BaseValue = 200f;
            attributeSet.Health.BaseValue = attributeSet.MaxHealth.BaseValue;
            attributeSet.Armor.BaseValue = 5f;
            attributeSet.Mana.BaseValue = 100f;
            attributeSet.MaxMana.BaseValue = 100f;
            attributeSet.ManaRegen.BaseValue = 2f; // 2 mana per
            attributeSet.CriticalChance.BaseValue = 0.1f; // 10% crit chance
            attributeSet.CriticalMultiplier.BaseValue = 2f; // 2x crit damage
            attributeSet.BaseDamage.BaseValue = 15f;
        }

        protected override void Update()
        {
            base.Update();
            // Enemy specific update logic
            UpdateBehavior();
        }

        protected virtual void UpdateBehavior()
        {
            // AI behavior logic
            if (target != null)
            {
                float distance = Vector3.Distance(transform.position, target.position);
                
                if (distance <= attackRange)
                {
                    Attack();
                }
                else if (distance <= detectionRange)
                {
                    MoveTowardsTarget();
                }
            }
        }

        protected virtual void MoveTowardsTarget()
        {
            // Movement logic
        }

        protected virtual void Attack()
        {
            // Attack logic
        }

        protected override void HandleAttributeChanged(AttributeChangeInfo changeInfo)
        {
            base.HandleAttributeChanged(changeInfo);

            if (changeInfo.AttributeType == EGameplayAttributeType.Health)
            {
                // Show damage popup for damage taken
                if (changeInfo.ChangeAmount < 0f)
                {
                    var popup = ResolveDamagePopupManager();
                    if (popup != null)
                    {
                        popup.ShowDamage(transform, Mathf.Abs(changeInfo.ChangeAmount));
                    }
                }

                // Check for death
                if (changeInfo.NewValue <= 0f)
                {
                    OnDeath();
                }
            }
        }

        protected virtual void OnDeath()
        {
            Debug.Log($"[EnemyBase] {name} died!");
            Destroy(gameObject);
        }
        private DamagePopupManager ResolveDamagePopupManager()
        {
            if (damagePopupManager != null)
            {
                return damagePopupManager;
            }

#if UNITY_2023_1_OR_NEWER
            damagePopupManager = FindFirstObjectByType<DamagePopupManager>();
#else
            damagePopupManager = FindObjectOfType<DamagePopupManager>();
#endif
            return damagePopupManager;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
    }
}
