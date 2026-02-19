using Unity.Mathematics;

namespace Abel.TowerDefense.Data
{
    public struct BulletLogicData
    {
        public float2 direction; // Hướng bay chuẩn hóa (Normalized)
        public float lifetime;   // Thời gian sống (để tự hủy nếu bay ra ngoài map)
    }
}