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