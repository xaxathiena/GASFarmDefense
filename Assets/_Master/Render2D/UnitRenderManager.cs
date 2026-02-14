using UnityEngine;
using System.Collections.Generic;

public class UnitRenderManager : MonoBehaviour
{
    [Header("1. Assets Cần Thiết")]
    public Mesh quadMesh;              // Chọn Mesh "Quad" của Unity
    public Material instanceMaterial;  // Material dùng Shader Instanced
    
    [Header("2. Dữ Liệu Tự Động (Từ Baker)")]
    public UnitAnimData unitData;      // Kéo file _Data (ScriptableObject) vào đây

    [Header("3. Cấu Hình Spawning")]
    public int unitCount = 5000;       // Số lượng unit muốn vẽ
    public float spawnRadius = 50f;    // Bán kính rải quân
    
    [Header("4. Cấu Hình Hiển Thị")]
    [Range(0.1f, 10.0f)] 
    public float unitScale = 5.0f;     // <--- BIẾN MỚI: Kéo to nhỏ tại đây
    
    [Range(0.1f, 5.0f)]
    public float globalSpeed = 1.0f;   // Tốc độ animation toàn cục

    // --- CÁC BIẾN NỘI BỘ (PRIVATE) ---
    private RenderParams renderParams;
    private Matrix4x4[] matrices;
    private float[] frameIndices;      // Mảng chứa frame hiện tại của từng con
    
    // Struct giả lập con quái (thay vì dùng GameObject nặng nề)
    struct VirtualUnit
    {
        public Vector3 position;
        public Vector3 velocity;
        public int currentAnimIndex; // Đang chạy clip nào (0: Attack, 1: Idle...)
        public float animTimer;      // Thời gian chạy của anim đó
    }
    private VirtualUnit[] units;

    void Start()
    {
        // 1. Kiểm tra dữ liệu đầu vào
        if (unitData == null) { Debug.LogError("Thiếu Unit Data! Hãy kéo file _Data vào."); return; }
        if (quadMesh == null) { Debug.LogError("Thiếu Quad Mesh!"); return; }
        if (instanceMaterial == null) { Debug.LogError("Thiếu Material!"); return; }

        // 2. Tự động gán Texture Array vào Material (đỡ phải làm tay)
        instanceMaterial.SetTexture("_MainTexArray", unitData.textureArray);

        // 3. Setup RenderParams (Cấu hình cho GPU Instancing)
        renderParams = new RenderParams(instanceMaterial);
        renderParams.matProps = new MaterialPropertyBlock();
        renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 10000); // Vùng vẽ vô tận
        renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderParams.receiveShadows = true;

        // 4. Khởi tạo mảng dữ liệu
        matrices = new Matrix4x4[unitCount];
        frameIndices = new float[unitCount];
        units = new VirtualUnit[unitCount];

        // 5. Spawn unit ngẫu nhiên
        for (int i = 0; i < unitCount; i++)
        {
            // Vị trí ngẫu nhiên trong hình tròn
            units[i].position = Random.insideUnitCircle * spawnRadius;
            
            // Tốc độ di chuyển ngẫu nhiên
            units[i].velocity = Random.insideUnitCircle * 2f; 
            
            // Chọn ngẫu nhiên 1 animation từ list (VD: Attack hoặc Idle)
            units[i].currentAnimIndex = Random.Range(0, unitData.animations.Count);
            
            // Random thời gian bắt đầu để chúng không bị "đồng diễn"
            units[i].animTimer = Random.Range(0f, 10f);
            
            // Setup Matrix ban đầu
            matrices[i] = Matrix4x4.TRS(units[i].position, Quaternion.identity, Vector3.one * unitScale);
        }
    }

    void Update()
    {
        if (unitData == null) return;

        // Vòng lặp cập nhật Logic cho từng con (Chạy trên CPU)
        for (int i = 0; i < unitCount; i++)
        {
            // A. DI CHUYỂN GIẢ LẬP
            units[i].position += units[i].velocity * Time.deltaTime;
            
            // Nếu đi ra xa quá thì quay đầu lại (để không chạy mất)
            if (units[i].position.magnitude > spawnRadius + 10f)
            {
                units[i].velocity = -units[i].position.normalized * 2f;
            }

            // B. TÍNH TOÁN ANIMATION
            // Lấy thông tin từ file Data (StartFrame, FrameCount...)
            var animInfo = unitData.animations[units[i].currentAnimIndex];
            
            // Tăng timer
            units[i].animTimer += Time.deltaTime * animInfo.fps * globalSpeed;
            
            // Tính Frame hiện tại: Start + (Timer % Length)
            float currentFrame = animInfo.startFrame + (units[i].animTimer % animInfo.frameCount);
            
            // C. CẬP NHẬT MATRIX (VỊ TRÍ & SCALE)
            // Quay mặt theo hướng di chuyển (Flip X)
            Quaternion rotation = units[i].velocity.x > 0 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
            
            // *** QUAN TRỌNG: Áp dụng unitScale vào đây ***
            matrices[i].SetTRS(units[i].position, rotation, Vector3.one * unitScale);

            // Lưu frame vào mảng để gửi xuống GPU
            frameIndices[i] = currentFrame;
        }

        // 6. RENDER (GỬI LỆNH VẼ XUỐNG GPU)
        // Gửi mảng Frame Index
        renderParams.matProps.SetFloatArray("_FrameIndex", frameIndices);
        
        // Vẽ 1 lần (Batch) cho tất cả unit
        Graphics.RenderMeshInstanced(renderParams, quadMesh, 0, matrices, unitCount);
    }

    // Vẽ Gizmos để debug vị trí nếu unit bị tàng hình
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Vector3.zero, spawnRadius);
    }
}