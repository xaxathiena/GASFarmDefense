using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Abel.TowerDefense.Data;
using Abel.TowerDefense.Config;
using Abel.TowerDefense.Render;
using Unity.Burst;

namespace Abel.TowerDefense.Core
{
    public abstract class UnitGroupBase : System.IDisposable
    {
        public readonly int MaxCapacity;
        public int ActiveCount { get; protected set; } = 0;

        public NativeArray<UnitRenderData> RenderData;

        protected UnitBatchRenderer renderer;
        protected UnitRenderProfileData profile;
        public string GroupName => profile.unitID; // For debugging/UI

        public UnitGroupBase(UnitRenderProfileData profile)
        {
            this.profile = profile;
            this.MaxCapacity = profile.maxCapacity;
            RenderData = new NativeArray<UnitRenderData>(this.MaxCapacity, Allocator.Persistent);
            renderer = new UnitBatchRenderer(profile, this.MaxCapacity);
        }

        public virtual void Spawn(Vector2 position) { }

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
        protected virtual void RemoveUnitAt(int index)
        {
            // if (index < 0 || index >= ActiveCount) return;

            // RenderData[index] = RenderData[ActiveCount - 1];
            // LogicData[index] = LogicData[ActiveCount - 1];

            // ActiveCount--;
        }
    }
    [BurstCompile]
    public struct UpdateAnimationTimerJob : IJobParallelFor
    {
        public float deltaTime;
        public NativeArray<UnitRenderData> data;

        public void Execute(int i)
        {
            // Structs are value types, so we copy, modify, and write back.
            var unit = data[i];
            unit.animTimer += deltaTime;
            data[i] = unit;
        }
    }
}