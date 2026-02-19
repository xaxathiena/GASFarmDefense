using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Abel.TowerDefense.Data;
using Abel.TowerDefense.Config;
using Abel.TowerDefense.Render;

namespace Abel.TowerDefense.Core
{
    public abstract class UnitGroupBase : System.IDisposable
    {
        protected const int MAX_CAPACITY = 10000;
        public int ActiveCount { get; protected set; } = 0;

        public NativeArray<UnitRenderData> RenderData;

        protected UnitBatchRenderer renderer;
        protected UnitProfileData profile;
        public string GroupName => profile.unitID; // For debugging/UI

        public UnitGroupBase(UnitProfileData profile)
        {
            this.profile = profile;

            RenderData = new NativeArray<UnitRenderData>(MAX_CAPACITY, Allocator.Persistent);

            renderer = new UnitBatchRenderer(profile, MAX_CAPACITY);
        }

        public virtual void Spawn(Vector2 position){}

        public virtual void Update(float dt)
        {
            if (ActiveCount == 0) return;

            // 1. Logic riêng của từng loại Unit (Lớp con implement)
            OnUpdateLogic(dt);

            // 2. Animation Timer (Chung)
            var animJob = new UpdateAnimationTimerJob { deltaTime = dt, data = RenderData };
            animJob.Schedule(ActiveCount, 64).Complete();

            // 3. Render (Chung)
            renderer.Render(RenderData, ActiveCount);
        }

        public virtual void Dispose()
        {
            if (RenderData.IsCreated) RenderData.Dispose();
            if (renderer != null) renderer.Dispose();
        }

        // --- CÁC HÀM CON PHẢI IMPLEMENT ---
        protected abstract void OnSpawnLogic(ref UnitLogicData logic, ref UnitRenderData render);
        protected abstract void OnUpdateLogic(float dt);
        /// <summary>
        /// Helper to find a unit closest to a world position.
        /// Returns the Index of the unit, or -1 if none found.
        /// </summary>
        public int FindUnitIndexAt(Vector2 worldPos, float radiusThreshold = 1.0f)
        {
            float minDistSq = radiusThreshold * radiusThreshold;
            int foundIndex = -1;
            for (int i = 0; i < ActiveCount; i++)
            {
                var unitPos = RenderData[i].position;
                // Calculate distance squared (faster than distance)
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
    }
}