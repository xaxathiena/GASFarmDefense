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

        // Mảng nội bộ của Render để duy trì Animation Timer
        private NativeArray<UnitRenderData> renderData;
        private UnitBatchRenderer batchRenderer;
    
        public RenderGroup(UnitRenderProfileData profile)
        {
            this.MaxCapacity = profile.maxCapacity;
            renderData = new NativeArray<UnitRenderData>(this.MaxCapacity, Allocator.Persistent);
            batchRenderer = new UnitBatchRenderer(profile, this.MaxCapacity);
        }

        /// <summary>
        /// Hàm này được System A gọi mỗi frame.
        /// </summary>
        public void SyncAndRender(NativeArray<UnitSyncData> incomingData, int activeCount, float dt)
        {
            if (activeCount == 0) return;

            // Job: Copy Data từ Logic sang Render + Tính toán Animation
            var syncJob = new SyncAndAnimateJob
            {
                dt = dt,
                inData = incomingData,
                outRenderData = renderData
            };

            syncJob.Schedule(activeCount, 64).Complete();

            // Vẽ lên màn hình
            batchRenderer.Render(renderData, activeCount);
        }

        public void Dispose()
        {
            if (renderData.IsCreated) renderData.Dispose();
            if (batchRenderer != null) batchRenderer.Dispose();
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
                var render = outRenderData[i]; // Lấy render data cũ để giữ lại animTimer
                render.instanceID = sync.instanceID;
                // 1. Cập nhật vị trí, góc xoay từ Logic
                render.instanceID = sync.instanceID; // Required for UnitDebugger click selection
                render.position = sync.position;
                render.rotation = sync.rotation;
                render.scale = sync.scale;
                render.playSpeed = sync.playSpeed;

                // 2. Logic Animation Timer
                if (render.animIndex != sync.animIndex)
                {
                    // Logic vừa đổi state (VD: Idle sang Attack) -> Reset timer
                    render.animIndex = sync.animIndex;
                    render.animTimer = 0f;
                }
                else
                {
                    // Vẫn state cũ -> Tăng thời gian
                    render.animTimer += dt;
                }

                // 3. Lưu lại
                outRenderData[i] = render;
            }
        }
    }
}