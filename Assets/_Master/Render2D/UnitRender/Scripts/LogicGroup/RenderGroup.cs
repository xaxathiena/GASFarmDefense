using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Abel.TowerDefense.Data;
using Abel.TowerDefense.Config;

namespace Abel.TowerDefense.Render
{
    public class RenderGroup : System.IDisposable
    {
        public readonly int MaxCapacity;

        // Internal render data array – persists between frames to keep animTimer state.
        private NativeArray<UnitRenderData> renderData;
        private UnitBatchRenderer batchRenderer;
        private HealthBarBatchRenderer hpBatchRenderer;

        /// <summary>
        /// Creates a RenderGroup for one unit type.
        /// </summary>
        /// <param name="profile">Sprite/animation profile for this unit type.</param>
        /// <param name="hpQuadMesh">Shared quad mesh used for all health bars.</param>
        /// <param name="hpMaterial">Material that reads _HPPercent and draws the health bar.</param>
        public RenderGroup(UnitRenderProfileData profile, UnityEngine.Mesh hpQuadMesh, UnityEngine.Material hpMaterial)
        {
            this.MaxCapacity = profile.maxCapacity;
            renderData = new NativeArray<UnitRenderData>(this.MaxCapacity, Allocator.Persistent);
            batchRenderer = new UnitBatchRenderer(profile, this.MaxCapacity);
            if (profile.showHealthBar)
            {

                hpBatchRenderer = new HealthBarBatchRenderer(hpQuadMesh, hpMaterial, this.MaxCapacity);
            }
        }

        /// <summary>
        /// Called every frame by the Logic system (System A).
        /// Runs the sync job, then issues both the unit draw call and the health-bar draw call.
        /// </summary>
        public void SyncAndRender(NativeArray<UnitSyncData> incomingData, int activeCount, float dt)
        {
            if (activeCount == 0) return;

            // Job: copy position/anim/hp data from the logic buffer into the render buffer.
            var syncJob = new SyncAndAnimateJob
            {
                dt = dt,
                inData = incomingData,
                outRenderData = renderData
            };

            syncJob.Schedule(activeCount, 64).Complete();

            // Draw unit sprites.
            batchRenderer.Render(renderData, activeCount);

            // Draw health bars only if this unit type has them enabled.
            hpBatchRenderer?.Render(renderData, activeCount);
        }

        public void Dispose()
        {
            if (renderData.IsCreated) renderData.Dispose();
            if (batchRenderer != null) batchRenderer.Dispose();
            if (hpBatchRenderer != null) hpBatchRenderer.Dispose();
        }
        /// <summary>
        /// Finds the index of a unit within a specific radius. Returns -1 if not found.
        /// </summary>
        public int FindUnitIndexAt(UnityEngine.Vector2 worldPos, float radiusThreshold = 1.0f)
        {
            float minDistSq = radiusThreshold * radiusThreshold;
            int foundIndex = -1;

            // Loop through active count to find closest unit
            // Note: If you need access to activeCount, you might need to store it locally in RenderGroup 
            // during SyncAndRender, or pass it in. For now, we assume you added an ActiveCount property.
            for (int i = 0; i < renderData.Length; i++)
            {
                // Note: Only check valid instances. A simple way is checking if scale > 0 or a valid ID
                if (renderData[i].instanceID == 0) continue;

                var unitPos = renderData[i].position;
                float dx = unitPos.x - worldPos.x;
                float dy = unitPos.y - worldPos.y;
                float distSq = dx * dx + dy * dy;

                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    foundIndex = i;
                }
            }
            return foundIndex;
        }

        // Expose RenderData for the Debugger
        public Unity.Collections.NativeArray<Abel.TowerDefense.Data.UnitRenderData> GetRenderData() => renderData;
        [BurstCompile]
        private struct SyncAndAnimateJob : IJobParallelFor
        {
            public float dt;
            [ReadOnly] public NativeArray<UnitSyncData> inData;
            public NativeArray<UnitRenderData> outRenderData;

            public void Execute(int i)
            {
                var sync = inData[i];
                // Preserve the previous render state so animTimer survives between frames.
                var render = outRenderData[i];

                // 1. Copy transform data from the logic buffer.
                render.instanceID = sync.instanceID; // Required for UnitDebugger click selection.
                render.position = sync.position;
                render.rotation = sync.rotation;
                render.scale = sync.scale;
                render.playSpeed = sync.playSpeed;

                // 2. Copy normalized health so the health-bar renderer can read it directly.
                render.hpPercent = sync.hpPercent;

                // 3. Animation timer: reset on state transition, accumulate otherwise.
                if (render.animIndex != sync.animIndex)
                {
                    // The logic system changed the animation state – restart from the beginning.
                    render.animIndex = sync.animIndex;
                    render.animTimer = 0f;
                }
                else
                {
                    // Same state – advance the timer.
                    render.animTimer += dt;
                }

                // Write result back to the persistent render buffer.
                outRenderData[i] = render;
            }
        }
    }
}