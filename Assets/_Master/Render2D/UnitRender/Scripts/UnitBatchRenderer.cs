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

        public UnitBatchRenderer(UnitRenderProfileData profile, int capacity)
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
                // Floor to integer slice index to prevent tri-linear blending between
                // adjacent Texture2DArray slices (which causes horizontal stripe artifacts).
                float currentFrame = Mathf.Floor(info.startFrame + (u.animTimer * speed) % info.frameCount);

                // 2. Create Matrix (TRS)
                // Tilt the quad to perfectly face the 45-degree orthographic camera (Billboard effect)
                float cameraTilt = 45f; 
                Quaternion rot = Quaternion.Euler(cameraTilt, 0, 0);
                
                // Map the logical 2D position to the 3D XZ plane
                Vector3 pos = new Vector3(u.position.x, 0, u.position.y);
                
                // Retrieve safe scale and aspect ratio values
                float safeAspectRatio = (info.aspectRatio > 0.01f) ? info.aspectRatio : 1.0f;
                float infoScale = (info.scale > 0.01f) ? info.scale : 1.0f;
                float currentScale = (u.scale > 0.01f) ? u.scale : 1.0f;
                float baseScale = currentScale * infoScale;
                
                // --- DIRECTION & FLIP X LOGIC ---
                // Normalize rotation to 0-360 degrees
                float normalizedRot = u.rotation % 360f;
                if (normalizedRot < 0) normalizedRot += 360f;
                
                // Assuming 0 is UP (+Z), 90 is RIGHT (+X), 180 is DOWN (-Z), 270 is LEFT (-X)
                // If the unit is facing the left hemisphere (between 180 and 360), flip the X scale
                float flipMultiplier = (normalizedRot > 180f) ? -1f : 1f;

                // Apply flip and aspect ratio to the final scale
                Vector3 finalScale = new Vector3(safeAspectRatio * baseScale * flipMultiplier, baseScale, 1f);

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