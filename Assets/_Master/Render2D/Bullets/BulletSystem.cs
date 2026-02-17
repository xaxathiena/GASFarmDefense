using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections.Generic;

public class BulletSystem : MonoBehaviour, System.IDisposable
{
    [Header("Settings")]
    public Mesh bulletMesh;         // Kéo Mesh Quad vào
    public Material bulletMaterial; // Kéo Material Instanced vào
    public UnitAnimData bulletData; // Kéo file Bullets_Data vừa tạo vào

    // --- MEMORY ---
    private NativeArray<BulletData> bulletArray;
    private Matrix4x4[] matrixArray; // Array gửi cho GPU
    private float[] frameArray;      // Array frame (luôn là 0 vì đạn ko có anim)
    
    private int activeCount = 0;     // Số lượng đạn đang sống
    private const int MAX_BULLETS = 10000;

    // --- RENDER ---
    private RenderParams renderParams;

    // Setup (VContainer sẽ gọi hoặc Start gọi)
    public void Initialize()
    {
        // Cấp phát bộ nhớ 1 lần duy nhất
        bulletArray = new NativeArray<BulletData>(MAX_BULLETS, Allocator.Persistent);
        matrixArray = new Matrix4x4[MAX_BULLETS];
        frameArray = new float[MAX_BULLETS]; // Mặc định là 0 hết

        // Setup Render Material
        Material mat = new Material(bulletMaterial);
        mat.SetTexture("_MainTexArray", bulletData.textureArray);
        
        renderParams = new RenderParams(mat);
        renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 10000);
    }

    void Start()
    {
        Initialize();
    }

    // Hàm bắn đạn (Gun sẽ gọi hàm này)
    public void SpawnBullet(Vector2 startPos, Vector2 direction, float speed)
    {
        if (activeCount >= MAX_BULLETS) return; // Full đạn

        // Ghi vào cuối danh sách active
        bulletArray[activeCount] = new BulletData
        {
            position = new float2(startPos.x, startPos.y),
            direction = new float2(direction.x, direction.y),
            speed = speed,
            lifetime = 3.0f // Đạn sống 3 giây
        };

        activeCount++;
    }

    void Update()
    {
        if (activeCount == 0) return;

        // 1. CHẠY JOB UPDATE VỊ TRÍ
        BulletUpdateJob job = new BulletUpdateJob
        {
            deltaTime = Time.deltaTime,
            bullets = bulletArray
        };
        JobHandle handle = job.Schedule(activeCount, 64);
        handle.Complete(); // Đợi xong ngay để vẽ (Sync)

        // 2. LỌC ĐẠN CHẾT (SWAP-BACK ALGORITHM)
        // Kỹ thuật này cực nhanh để xóa phần tử trong mảng
        for (int i = activeCount - 1; i >= 0; i--)
        {
            if (bulletArray[i].lifetime <= 0)
            {
                // Nếu đạn chết -> Lấy con cuối cùng đè lên vị trí con chết
                activeCount--;
                bulletArray[i] = bulletArray[activeCount];
            }
        }

        // 3. UPDATE MATRIX ĐỂ VẼ
        // (Bước này có thể đưa vào Job nốt, nhưng làm ở đây cho dễ hiểu trước)
        for (int i = 0; i < activeCount; i++)
        {
            BulletData b = bulletArray[i];
            
            // Logic: Đạn hướng nào thì xoay hình theo hướng đó
            float angle = math.degrees(math.atan2(b.direction.y, b.direction.x));
            Quaternion rot = Quaternion.Euler(90, 0, -angle + 90); // +90 tùy hướng vẽ gốc của sprite

            // Kéo dãn viên đạn theo tốc độ (Stretched Quad - như đã bàn)
            // Càng nhanh càng dài ra
            float stretch = 1.0f + (b.speed * 0.05f);
            Vector3 scale = new Vector3(0.5f, stretch * 0.5f, 1f); // 0.5 là kích thước gốc

            matrixArray[i].SetTRS(new Vector3(b.position.x, 0, b.position.y), rot, scale);
        }

        // 4. RENDER
        renderParams.matProps.SetFloatArray("_FrameIndex", frameArray); // Frame 0
        Graphics.RenderMeshInstanced(renderParams, bulletMesh, 0, matrixArray, activeCount);
    }

    public void Dispose()
    {
        if (bulletArray.IsCreated) bulletArray.Dispose();
    }
    
    private void OnDestroy() { Dispose(); }
}