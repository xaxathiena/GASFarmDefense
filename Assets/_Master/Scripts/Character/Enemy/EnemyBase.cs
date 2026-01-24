using UnityEngine;

namespace FD.Character
{
    public abstract class EnemyBase : BaseCharacter
    {
        [Header("Enemy Specific")]
        [SerializeField] protected float detectionRange = 10f;
        [SerializeField] protected float attackRange = 2f;
        [SerializeField] protected Transform target;

        protected override void Awake()
        {
            base.Awake();
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

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
    }
}
