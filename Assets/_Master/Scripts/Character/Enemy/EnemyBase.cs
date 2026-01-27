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

        protected override void Start()
        {
            base.Start();
            // Enemy specific start logic
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

            if (changeInfo.AttributeType == EGameplayAttributeType.Health && changeInfo.ChangeAmount < 0f)
            {
                var popup = ResolveDamagePopupManager();
                if (popup != null)
                {
                    popup.ShowDamage(transform, Mathf.Abs(changeInfo.ChangeAmount));
                }
            }
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
