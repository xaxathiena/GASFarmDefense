using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Abel.TowerDefense.Config; // Reference to Config namespace
using Abel.TowerDefense.Data;   // Reference to Data namespace

namespace Abel.TowerDefense.Render
{
    /// <summary>
    /// Handles the conversion of raw UnitRenderData into GPU-compatible matrices
    /// and issues the DrawMeshInstanced command.
    /// </summary>
    public class UnitBatchRenderer : System.IDisposable
    {
        private Mesh mesh;
        private Material material; // Instance material for this specific unit type
        private UnitAnimData animData;
        
        // GPU Buffers
        private RenderParams renderParams;
        private Matrix4x4[] matrices;
        private float[] frameIndices;

        public UnitBatchRenderer(UnitProfile profile, int capacity)
        {
            this.mesh = profile.mesh;
            this.animData = profile.animData;

            // Clone the material to assign a specific Texture Array
            this.material = new Material(profile.baseMaterial);
            this.material.SetTexture("_MainTexArray", animData.textureArray);

            // Initialize Arrays (Pre-allocate memory)
            matrices = new Matrix4x4[capacity];
            frameIndices = new float[capacity];

            // Setup RenderParams for GPU Instancing
            renderParams = new RenderParams(this.material);
            renderParams.matProps = new MaterialPropertyBlock();
            // Set a large bound to prevent culling issues
            renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        }

        /// <summary>
        /// Main render loop: Converts RenderData -> GPU Data -> Draw Command.
        /// </summary>
        public void Render(NativeArray<UnitRenderData> units, int count)
        {
            for (int i = 0; i < count; i++)
            {
                UnitRenderData u = units[i];

                // 1. Calculate Animation Frame
                // Ensure the animIndex is valid to prevent out-of-bounds errors
                int safeAnimIndex = Mathf.Clamp(u.animIndex, 0, animData.animations.Count - 1);
                var info = animData.animations[safeAnimIndex];
                
                // Formula: StartFrame + (Timer * FPS * Modifiers) % FrameCount
                float speed = info.fps * info.speedModifier * u.playSpeed;
                float currentFrame = info.startFrame + (u.animTimer * speed) % info.frameCount;

                // 2. Create Matrix (TRS)
                // Note: Using negative Z rotation for correct 2D orientation on the XZ plane
                Quaternion rot = Quaternion.Euler(90, 0, -u.rotation);
                Vector3 pos = new Vector3(u.position.x, 0, u.position.y);
                
                // Combine unit scale with animation specific scale (e.g., boss scaling)
                Vector3 finalScale = Vector3.one * u.scale * info.scale; 

                matrices[i].SetTRS(pos, rot, finalScale);
                frameIndices[i] = currentFrame;
            }

            // 3. Send Data to GPU
            renderParams.matProps.SetFloatArray("_FrameIndex", frameIndices);
            Graphics.RenderMeshInstanced(renderParams, mesh, 0, matrices, count);
        }

        public void Dispose()
        {
            if (material != null) Object.Destroy(material);
        }
    }
}