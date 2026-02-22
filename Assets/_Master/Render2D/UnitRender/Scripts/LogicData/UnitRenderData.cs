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
        public int instanceID;
        // 1. Transform Data (Position, Rotation, Scale)
        public float2 position;
        public float rotation; // In Degrees
        public float scale;

        // 2. Animation State
        public int animIndex;   // Index of the animation clip (0: Idle, 1: Run...)
        public float animTimer; // Accumulated time for the current animation
        public float playSpeed; // Animation playback speed multiplier (1.0 = normal)
    }
}