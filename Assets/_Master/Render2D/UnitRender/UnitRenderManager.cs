using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Abel.TowerDefense.Data;   // Reference to Data namespace
using Abel.TowerDefense.Config; // Reference to Config namespace

namespace Abel.TowerDefense.Render
{
    /// <summary>
    /// The main manager component that sits in the scene.
    /// It holds the persistent NativeArray data and schedules Jobs.
    /// </summary>
    public class UnitRenderManager : MonoBehaviour
    {
        [Header("Configuration")]
        public UnitProfile visualConfig;
        public int unitCount = 5000;

        // Data Storage (Native Memory)
        private NativeArray<UnitRenderData> unitsData;
        private UnitBatchRenderer renderer;

        void Start()
        {
            // 1. Allocate Persistent Memory (Must be Disposed later)
            unitsData = new NativeArray<UnitRenderData>(unitCount, Allocator.Persistent);
            
            // 2. Initialize the Renderer
            renderer = new UnitBatchRenderer(visualConfig, unitCount);

            // 3. Spawn Test Units (Random Distribution)
            // In a real game, this data would come from your Gameplay Logic system.
            for (int i = 0; i < unitCount; i++)
            {
                unitsData[i] = new UnitRenderData
                {
                    position = new float2(UnityEngine.Random.Range(-50, 50), UnityEngine.Random.Range(-50, 50)),
                    rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360)).eulerAngles.z,
                    scale = 1.0f,
                    animIndex = 0, // Default to 0 (usually Idle or Run)
                    animTimer = UnityEngine.Random.Range(0f, 10f), // Random start time
                    playSpeed = 1.0f
                };
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // --- 1. SCHEDULE ANIMATION JOB ---
            // This job updates the 'animTimer' for all units in parallel.
            var animJob = new UpdateAnimationTimerJob
            {
                deltaTime = dt,
                data = unitsData
            };
            
            // Batch count 64 is a standard sweet spot for jobs
            JobHandle handle = animJob.Schedule(unitCount, 64);
            
            // For now, we complete immediately to render this frame.
            handle.Complete();

            // --- 2. RENDER ---
            renderer.Render(unitsData, unitCount);
        }

        void OnDestroy()
        {
            // Critical: Always dispose NativeArrays to prevent memory leaks
            if (unitsData.IsCreated) unitsData.Dispose();
            
            if (renderer != null) renderer.Dispose();
        }
    }

    /// <summary>
    /// A simple Burst-compiled Job to increment animation timers.
    /// </summary>
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