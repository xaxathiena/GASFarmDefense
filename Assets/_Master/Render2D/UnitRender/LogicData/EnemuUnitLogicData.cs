using Unity.Mathematics;
using Abel.TowerDefense.Config;

namespace Abel.TowerDefense.Data
{
    [System.Serializable]
    public struct EnemyUnitLogicData
    {
        public UnitState currentState;
        public float stateTimer;
        public float attackSpeed;
        
        public int waypointIndex; // Which waypoint in the path are we currently moving towards?
    }
}