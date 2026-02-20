using Unity.Mathematics;
using Abel.TowerDefense.Config;
namespace Abel.TowerDefense.Data
{
    [System.Serializable]
    public struct UnitLogicData
    {
        public UnitState currentState; // Trạng thái hiện tại
        public float stateTimer; // Thời gian đã ở trong state này
        
        // Gameplay Stats (Demo)
        public float attackSpeed; // Tốc độ đánh (giây/lần)
        public float attackRange;
    }
}