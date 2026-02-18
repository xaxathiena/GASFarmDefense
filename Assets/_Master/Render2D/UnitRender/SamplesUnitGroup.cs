using Abel.TowerDefense.Core;
using Abel.TowerDefense.Data;
using Abel.TowerDefense.Config;
using Unity.Collections;
using UnityEngine;

namespace Abel.TowerDefense.Logic
{
    // Unit A: Chỉ đứng chơi loop anim
    public class SimpleLoopGroup : UnitGroupBase
    {
        public SimpleLoopGroup(UnitProfileData profile) : base(profile) { }

        protected override void OnSpawnLogic(ref UnitLogicData logic, ref UnitRenderData render)
        {
            render.animIndex = 0; // Luôn chạy anim đầu tiên
        }

        protected override void OnUpdateLogic(float dt)
        {
            // Không làm gì cả, render tự chạy
        }
    }

    // Unit B: Có FSM Idle/Attack
    public class AggressiveFSMGroup : UnitGroupBase
    {
        private int idleIdx;
        private int atkIdx;

        public AggressiveFSMGroup(UnitProfileData profile) : base(profile) 
        { 
             // Cache index để chạy nhanh
             // (Giả sử Animation Data có anim tên "Idle" và "Attack")
             // Nếu profile.animData null thì phải check null nhé
             idleIdx = 0; 
             atkIdx = 1;
        }

        protected override void OnSpawnLogic(ref UnitLogicData logic, ref UnitRenderData render)
        {
            logic.currentState = UnitState.Idle;
            render.animIndex = idleIdx;
        }

        protected override void OnUpdateLogic(float dt)
        {
            // Lưu ý: Có thể chuyển thành Job nếu muốn tối ưu hơn nữa
            for(int i=0; i<ActiveCount; i++)
            {
                var logic = LogicData[i];
                var render = RenderData[i];
                logic.stateTimer += dt;

                if(logic.currentState == UnitState.Idle && logic.stateTimer > 2.0f)
                {
                    logic.currentState = UnitState.Attack;
                    logic.stateTimer = 0;
                    render.animIndex = atkIdx;
                    render.animTimer = 0;
                }
                else if(logic.currentState == UnitState.Attack && logic.stateTimer > 1.0f) // Hardcode duration 1s
                {
                    logic.currentState = UnitState.Idle;
                    logic.stateTimer = 0;
                    render.animIndex = idleIdx;
                    render.animTimer = 0;
                }

                LogicData[i] = logic;
                RenderData[i] = render;
            }
        }
    }
}