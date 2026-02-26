using Unity.Mathematics;

namespace Abel.TowerDefense.Render
{
    /// <summary>
    /// Produced by the Logic system (System A) and pushed to the Render system (System B) every frame.
    /// </summary>
    public struct UnitSyncData
    {
        public int    instanceID;
        public float2 position;
        public float  rotation;
        public float  scale;
        public int    animIndex;  // Tells the render system which animation clip to play
        public float  playSpeed;
        public float  hpPercent;  // Normalized health value [0.0 .. 1.0] for health-bar rendering
    }
}