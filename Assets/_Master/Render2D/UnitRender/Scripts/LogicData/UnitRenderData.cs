using Unity.Mathematics;
using UnityEngine;

namespace Abel.TowerDefense.Data
{
    /// <summary>
    /// Pure data structure containing only what is necessary for rendering.
    /// Does not contain gameplay logic like HP, Mana, Damage, etc.
    /// </summary>
    [System.Serializable]
    public struct UnitRenderData
    {
        public int    instanceID;

        // 1. Transform Data (Position, Rotation, Scale)
        public float2 position;
        public float  rotation;   // In degrees
        public float  scale;

        // 2. Animation State
        public int   animIndex;   // Index of the animation clip (0: Idle, 1: Run ...)
        public float animTimer;   // Accumulated playback time for the current animation
        public float playSpeed;   // Playback speed multiplier (1.0 = normal speed)

        // 3. Health Display
        public float hpPercent;   // Normalized health value [0.0 .. 1.0] for health-bar rendering
    }
}