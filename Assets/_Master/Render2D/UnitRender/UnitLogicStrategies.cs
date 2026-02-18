using UnityEngine;
using Unity.Collections;
using Abel.TowerDefense.Data;
using Abel.TowerDefense.Config;

namespace Abel.TowerDefense.Logic
{
    public interface IUnitLogicStrategy
    {
        void OnSpawn(UnitProfile profile, ref UnitLogicData logic, ref UnitRenderData render);
        void UpdateLogic(float dt, UnitProfile profile, NativeArray<UnitLogicData> logicData, NativeArray<UnitRenderData> renderData, int count);
    }

    // --- LOGIC CHO UNIT A (Đơn giản, loop 1 anim) ---
    public class PassiveLoopLogic : IUnitLogicStrategy
    {
        public void OnSpawn(UnitProfile profile, ref UnitLogicData logic, ref UnitRenderData render)
        {
            // Unit A always plays Anim 0
            render.animIndex = 0;
        }

        public void UpdateLogic(float dt, UnitProfile profile, NativeArray<UnitLogicData> logicData, NativeArray<UnitRenderData> renderData, int count)
        {
            // Unit A does nothing logically. Just exists. 
            // Animation timer is handled by the Job in UnitGroup.
        }
    }

    // --- LOGIC CHO UNIT B (FSM: Idle <-> Attack) ---
    public class AggressiveFSMLogic : IUnitLogicStrategy
    {
        // Cache mapping to avoid lookup every frame
        private int idleAnimIdx = -1;
        private int attackAnimIdx = -1;

        public void OnSpawn(UnitProfile profile, ref UnitLogicData logic, ref UnitRenderData render)
        {
            // Cache indices on first spawn if needed, or look up now
            if (idleAnimIdx == -1) idleAnimIdx = profile.GetAnimIndex(UnitState.Idle);
            if (attackAnimIdx == -1) attackAnimIdx = profile.GetAnimIndex(UnitState.Attack);

            // Start at Idle
            logic.currentState = UnitState.Idle;
            render.animIndex = idleAnimIdx;
        }

        public void UpdateLogic(float dt, UnitProfile profile, NativeArray<UnitLogicData> logicData, NativeArray<UnitRenderData> renderData, int count)
        {
            // Note: In a real project, this loop should be a Burst Job.
            // But for learning FSM flow, C# loop is fine for < 5000 units.
            for (int i = 0; i < count; i++)
            {
                var logic = logicData[i];
                var render = renderData[i];

                logic.stateTimer += dt;

                switch (logic.currentState)
                {
                    case UnitState.Idle:
                        // Switch to Attack after 2 seconds
                        if (logic.stateTimer > 2.0f)
                        {
                            logic.currentState = UnitState.Attack;
                            logic.stateTimer = 0;
                            render.animIndex = attackAnimIdx;
                            render.animTimer = 0;
                        }
                        break;

                    case UnitState.Attack:
                        // Check if animation finished
                        var animInfo = profile.animData.animations[attackAnimIdx];
                        if (logic.stateTimer > animInfo.duration)
                        {
                            logic.currentState = UnitState.Idle;
                            logic.stateTimer = 0;
                            render.animIndex = idleAnimIdx;
                            render.animTimer = 0;
                        }
                        break;
                }

                // Write back
                logicData[i] = logic;
                renderData[i] = render;
            }
        }
    }
}