using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

// 1. Dữ liệu thuần của 1 viên đạn (Struct = Value Type = Nhanh)
public struct BulletData
{
    public float2 position;
    public float2 direction; // Hướng bay (Normalized)
    public float speed;
    public float lifetime;   // Thời gian sống còn lại
}

// 2. Job tính toán di chuyển (Chạy đa luồng)
[BurstCompile]
public struct BulletUpdateJob : IJobParallelFor
{
    public float deltaTime;
    public NativeArray<BulletData> bullets;

    public void Execute(int i)
    {
        var b = bullets[i];
        
        // Di chuyển: Pos = Pos + Dir * Speed * dt
        b.position += b.direction * b.speed * deltaTime;
        
        // Trừ tuổi thọ
        b.lifetime -= deltaTime;
        
        bullets[i] = b;
    }
}
[BurstCompile]
public struct BulletFilterJob : IJob
{
    public NativeArray<BulletData> bullets;
    
    // Dùng NativeReference để Job có thể thay đổi biến activeCount thực tế
    public NativeReference<int> activeCountRef; 

    public void Execute()
    {
        int count = activeCountRef.Value;

        // Duyệt ngược để Swap-Back
        for (int i = count - 1; i >= 0; i--)
        {
            if (bullets[i].lifetime <= 0)
            {
                // Logic Swap-Back: Lấy con cuối đè lên con chết
                count--;
                bullets[i] = bullets[count];
            }
        }

        // Ghi ngược lại số lượng mới
        activeCountRef.Value = count;
    }
}
[BurstCompile]
public struct BulletMatrixJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<BulletData> bullets;
    [WriteOnly] public NativeArray<Matrix4x4> matrices;

    public void Execute(int i)
    {
        var b = bullets[i];

        // Tính góc xoay
        float angle = math.degrees(math.atan2(b.direction.y, b.direction.x));
        Quaternion rot = Quaternion.Euler(90, 0, -angle + 90);

        // Tính độ giãn (Stretch)
        float stretch = 1.0f + (b.speed * 0.05f);
        Vector3 scale = new Vector3(0.5f, stretch * 0.5f, 1f);

        // Tạo Matrix
        matrices[i] = Matrix4x4.TRS(new Vector3(b.position.x, 0, b.position.y), rot, scale);
    }
}