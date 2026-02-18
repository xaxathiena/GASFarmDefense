using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class BulletSystem : MonoBehaviour, System.IDisposable
{
    [Header("Settings")]
    public Mesh bulletMesh;
    public Material bulletMaterial;
    public UnitAnimData bulletData;

    // --- MEMORY ---
    private NativeArray<BulletData> bulletArray;
    private NativeArray<Matrix4x4> matrixNativeArray; // Tính toán trên này
    private Matrix4x4[] matrixManagedArray;           // Copy ra đây để vẽ (API cũ cần cái này)
    private float[] frameArray;
    
    // Dùng NativeReference để chia sẻ biến đếm số lượng giữa C# và Job
    private NativeReference<int> activeCountRef; 
    
    private const int MAX_BULLETS = 10000;
    private RenderParams renderParams;

    public void Initialize()
    {
        bulletArray = new NativeArray<BulletData>(MAX_BULLETS, Allocator.Persistent);
        matrixNativeArray = new NativeArray<Matrix4x4>(MAX_BULLETS, Allocator.Persistent);
        matrixManagedArray = new Matrix4x4[MAX_BULLETS];
        frameArray = new float[MAX_BULLETS];
        
        // Khởi tạo biến đếm count
        activeCountRef = new NativeReference<int>(0, Allocator.Persistent);

        // Setup Render
        Material mat = new Material(bulletMaterial);
        mat.SetTexture("_MainTexArray", bulletData.textureArray);
        renderParams = new RenderParams(mat);
        renderParams.matProps = new MaterialPropertyBlock();
        renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 10000);
    }

    void Start() { Initialize(); }

    public void SpawnBullet(Vector2 startPos, Vector2 direction, float speed)
    {
        int count = activeCountRef.Value; // Lấy giá trị hiện tại
        if (count >= MAX_BULLETS) return;

        bulletArray[count] = new BulletData
        {
            position = new float2(startPos.x, startPos.y),
            direction = new float2(direction.x, direction.y),
            speed = speed,
            lifetime = 3.0f
        };

        activeCountRef.Value = count + 1; // Tăng số lượng
    }

    void Update()
    {
        int currentCount = activeCountRef.Value;
        if (currentCount == 0) return;

        // --- BƯỚC 1: JOB UPDATE VỊ TRÍ ---
        BulletUpdateJob updateJob = new BulletUpdateJob
        {
            deltaTime = Time.deltaTime,
            bullets = bulletArray
        };
        // Schedule trả về 1 cái handle (cờ hiệu)
        JobHandle updateHandle = updateJob.Schedule(currentCount, 64);

        // --- BƯỚC 2: JOB LỌC ĐẠN CHẾT ---
        // Job này phụ thuộc vào updateHandle (tức là Update xong mới được Lọc)
        BulletFilterJob filterJob = new BulletFilterJob
        {
            bullets = bulletArray,
            activeCountRef = activeCountRef
        };
        JobHandle filterHandle = filterJob.Schedule(updateHandle);

        // --- BƯỚC 3: JOB TÍNH MATRIX ---
        // Job này phụ thuộc vào filterHandle (Lọc xong, sắp xếp lại mảng rồi mới tính Matrix)
        // Lưu ý: Lúc schedule ta vẫn dùng currentCount cũ, nhưng bên trong Job ta chỉ quan tâm mảng đã được lọc
        // Để an toàn và tối ưu, ta có thể chấp nhận tính dư 1 chút (những con vừa chết) hoặc đợi complete.
        // Ở đây để tối đa hiệu năng, ta tính luôn ma trận dựa trên số lượng cũ (thừa vài con ko sao, vì tí nữa vẽ theo số lượng mới).
        
        BulletMatrixJob matrixJob = new BulletMatrixJob
        {
            bullets = bulletArray,
            matrices = matrixNativeArray
        };
        JobHandle finalHandle = matrixJob.Schedule(currentCount, 64, filterHandle);

        // --- BƯỚC 4: CHỜ TẤT CẢ HOÀN TẤT ---
        finalHandle.Complete(); 

        // --- BƯỚC 5: CHUẨN BỊ VẼ ---
        // Lấy số lượng thực tế sau khi đã lọc
        int finalCount = activeCountRef.Value;

        // Copy dữ liệu từ NativeArray sang ManagedArray để vẽ (Thao tác này rất nhanh: MemCpy)
        // Chỉ copy số lượng cần thiết
        if (finalCount > 0)
        {
            NativeArray<Matrix4x4>.Copy(matrixNativeArray, matrixManagedArray, finalCount);
            
            renderParams.matProps.SetFloatArray("_FrameIndex", frameArray);
            Graphics.RenderMeshInstanced(renderParams, bulletMesh, 0, matrixManagedArray, finalCount);
        }
    }

    public void Dispose()
    {
        if (bulletArray.IsCreated) bulletArray.Dispose();
        if (matrixNativeArray.IsCreated) matrixNativeArray.Dispose();
        if (activeCountRef.IsCreated) activeCountRef.Dispose();
    }
    
    private void OnDestroy() { Dispose(); }
}