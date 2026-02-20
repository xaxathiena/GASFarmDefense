using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Abel.TowerDefense.Core;
using Abel.TowerDefense.Data;
using Abel.TowerDefense.Config;

namespace Abel.TowerDefense.Logic
{
    public class BulletGroup : UnitGroupBase
    {
        // Define specific logic data for bullets
        public NativeArray<BulletLogicData> LogicData;

        // Custom config variable (can be pulled from profile if added later)
        private float moveSpeed = 20.0f; 

        public BulletGroup(UnitProfileData profile) : base(profile)
        {
            // Allocate memory for logic data
            LogicData = new NativeArray<BulletLogicData>(profile.maxCapacity, Allocator.Persistent);
            
            // Note: If you have baseMoveSpeed in UnitProfileData, you can map it here
            this.moveSpeed = profile.baseMoveSpeed; 
        }

        // Custom spawn for bullets requiring direction
        public void Spawn(Vector2 position, Vector2 direction)
        {
            if (ActiveCount >= MaxCapacity) return;
            int id = ActiveCount;

            // Setup Logic
            LogicData[id] = new BulletLogicData 
            { 
                direction = new float2(direction.x, direction.y),
                lifetime = 5.0f // 5 seconds to live
            };

            // Setup Render
            RenderData[id] = new UnitRenderData 
            {
                position = new float2(position.x, position.y),
                rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
                scale = 1.0f, 
                animIndex = 0, 
                animTimer = 0, 
                playSpeed = 1.0f
            };

            ActiveCount++;
        }

        protected override void OnUpdateLogic(float dt)
        {
            // Iterate backwards because we might remove elements
            for (int i = ActiveCount - 1; i >= 0; i--)
            {
                var logic = LogicData[i];
                var render = RenderData[i];

                logic.lifetime -= dt;

                if (logic.lifetime <= 0)
                {
                    RemoveUnitAt(i);
                    continue; // Skip the rest of the loop for this bullet
                }

                // Update position based on direction and speed
                render.position += logic.direction * moveSpeed * dt;

                // Write back
                LogicData[i] = logic;
                RenderData[i] = render;
            }
        }

        protected override void RemoveUnitAt(int index)
        {
            if (index < 0 || index >= ActiveCount) return;

            // 1. Swap the specific Logic Data FIRST
            LogicData[index] = LogicData[ActiveCount - 1];

            // 2. Let the base class swap RenderData and decrement ActiveCount
            base.RemoveUnitAt(index);
        }

        public override void Dispose()
        {
            // Clean up custom array
            if (LogicData.IsCreated) LogicData.Dispose();

            // Clean up base arrays and renderer
            base.Dispose();
        }
    }
}