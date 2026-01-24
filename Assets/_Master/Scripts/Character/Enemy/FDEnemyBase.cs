using System;
using UnityEngine;

namespace FD.Character
{
    public class FDEnemyBase : EnemyBase
    {
        [Header("Path Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float waypointReachedDistance = 0.1f;

        private Transform[] pathPoints;
        private int currentPathIndex;
        private bool hasPath;

        public event Action<FDEnemyBase> ReachedPathEnd;

        public void InitializePath(Transform[] newPathPoints)
        {
            pathPoints = newPathPoints;
            currentPathIndex = 0;
            hasPath = pathPoints != null && pathPoints.Length > 0;
        }

        protected override void UpdateBehavior()
        {
            if (hasPath)
            {
                MoveAlongPath();
                return;
            }

            base.UpdateBehavior();
        }

        private void MoveAlongPath()
        {
            if (pathPoints == null || pathPoints.Length == 0)
            {
                return;
            }

            if (currentPathIndex >= pathPoints.Length)
            {
                OnReachedPathEnd();
                return;
            }

            var target = pathPoints[currentPathIndex];
            if (target == null)
            {
                currentPathIndex++;
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, target.position) <= waypointReachedDistance)
            {
                currentPathIndex++;
            }

            if (currentPathIndex >= pathPoints.Length)
            {
                OnReachedPathEnd();
            }
        }

        private void OnReachedPathEnd()
        {
            ReachedPathEnd?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
