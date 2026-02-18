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
        public NativeArray<UnitLogicData> LogicData;

        protected UnitBatchRenderer renderer;
        protected UnitProfileData profile;
        public string GroupName => profile.unitID; // For debugging/UI

        public UnitGroupBase(UnitProfileData profile)
        {
            this.profile = profile;

            RenderData = new NativeArray<UnitRenderData>(MAX_CAPACITY, Allocator.Persistent);
            LogicData = new NativeArray<UnitLogicData>(MAX_CAPACITY, Allocator.Persistent);

            renderer = new UnitBatchRenderer(profile, MAX_CAPACITY);
        }

        public void Spawn(Vector2 position)
        {
            if (ActiveCount >= MAX_CAPACITY) return;
            int id = ActiveCount;

            // 1. Init Data cơ bản
            var logic = new UnitLogicData
            {
                currentState = UnitState.Idle,
                stateTimer = 0,
                attackSpeed = profile.baseAttackSpeed
            };

            var render = new UnitRenderData
            {
                position = new Unity.Mathematics.float2(position.x, position.y),
                rotation = 0,
                scale = 1.0f,
                animIndex = 0,
                animTimer = Random.Range(0f, 1f),
                playSpeed = 1.0f
            };

            // 2. Gọi hàm Abstract để lớp con xử lý logic riêng
            OnSpawnLogic(ref logic, ref render);

            // 3. Save
            LogicData[id] = logic;
            RenderData[id] = render;
            ActiveCount++;
        }

        public void Update(float dt)
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

        public void Dispose()
        {
            if (RenderData.IsCreated) RenderData.Dispose();
            if (LogicData.IsCreated) LogicData.Dispose();
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