using UnityEngine;
using Unity.Mathematics;
using Abel.TowerDefense.Core;
using Abel.TowerDefense.Data;
using Abel.TowerDefense.Config;
using Unity.Collections;

namespace Abel.TowerDefense.Logic
{
    public class EnemyFollowingGroup : UnitGroupBase
    {
        public Vector2[] pathWaypoints; // Mảng chứa đường đi
        public NativeArray<EnemyUnitLogicData> LogicData;

        public EnemyFollowingGroup(UnitProfileData profile) : base(profile)
        {
            LogicData = new NativeArray<EnemyUnitLogicData>(profile.maxCapacity, Allocator.Persistent);
        }
        public override void Spawn(Vector2 position)
        {
            if (ActiveCount >= MaxCapacity) return;
            int id = ActiveCount;

            // 1. Init Data cơ bản
            var logic = new EnemyUnitLogicData
            {
                currentState = UnitState.Idle,
                stateTimer = 0,
                attackSpeed = profile.baseAttackSpeed,
                waypointIndex = 0,
            };

            var render = new UnitRenderData
            {
                position = new Unity.Mathematics.float2(position.x, position.y),
                rotation = 0,
                scale = 1.0f,
                animIndex = 0,
                animTimer = UnityEngine.Random.Range(0f, 1f),
                playSpeed = 1.0f
            };

            // 2. Gọi hàm Abstract để lớp con xử lý logic riêng
            OnSpawnUnit(ref logic, ref render);

            // 3. Save
            LogicData[id] = logic;
            RenderData[id] = render;
            ActiveCount++;
        }
        protected void OnSpawnUnit(ref EnemyUnitLogicData logic, ref UnitRenderData render)
        {
            render.animIndex = 0; // Anim mặc định (Move/Run)
            logic.waypointIndex = 0; // Bắt đầu từ điểm số 0
        }

        protected override void OnUpdateLogic(float dt)
        {
            if (pathWaypoints == null || pathWaypoints.Length == 0) return;

            // DUYỆT NGƯỢC BẮT BUỘC (Vì có thao tác xóa)
            for (int i = ActiveCount - 1; i >= 0; i--)
            {
                var logic = LogicData[i];
                var render = RenderData[i];

                Vector2 targetPos = pathWaypoints[logic.waypointIndex];
                Vector2 currentPos = new Vector2(render.position.x, render.position.y);

                // Tính khoảng cách và hướng đi
                Vector2 dir = targetPos - currentPos;
                float distance = dir.magnitude;

                if (distance < 0.1f) // Đã tới Waypoint hiện tại
                {
                    logic.waypointIndex++; // Tăng mốc lên

                    // Nếu đã đi hết đường -> Tiêu diệt Unit
                    if (logic.waypointIndex >= pathWaypoints.Length)
                    {
                        RemoveUnitAt(i);
                        continue; // Bỏ qua đoạn code lưu Data bên dưới
                    }
                }
                else
                {
                    // Di chuyển
                    Vector2 moveDir = dir / distance; // Normalize
                    currentPos += moveDir * profile.baseMoveSpeed * dt;

                    render.position = new float2(currentPos.x, currentPos.y);

                    // Xoay mặt (Tùy chọn: Xoay lật trái phải theo trục X)
                    if (moveDir.x > 0) render.rotation = 0;
                    else if (moveDir.x < 0) render.rotation = 180;
                }

                // Ghi lại Data (Nếu chưa chết)
                LogicData[i] = logic;
                RenderData[i] = render;
            }
        }
        protected override void RemoveUnitAt(int index)
        {
            if (index < 0 || index >= ActiveCount) return;

            RenderData[index] = RenderData[ActiveCount - 1];
            LogicData[index] = LogicData[ActiveCount - 1];

            ActiveCount--;
        }
    }
}