using Unity.Mathematics;

namespace Abel.TowerDefense.Render
{
    /// <summary>
    /// Struct này do System A (Logic) tạo ra và đẩy sang cho System B (Render) mỗi frame.
    /// </summary>
    public struct UnitSyncData
    {
        public int instanceID;
        public float2 position;
        public float rotation;
        public float scale;
        public int animIndex; // Logic bảo: "Ê, chạy anim số 1 cho tao"
        public float playSpeed;
    }
}