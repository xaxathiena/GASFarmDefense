using UnityEngine;
using VContainer;

public class GunController : MonoBehaviour
{
    [Inject] private BulletSystem bulletSystem;

    [Header("Gun Stats")]
    public float fireRate = 0.1f;
    public float bulletSpeed = 20f;
    public float spread = 0.1f;
    
    [Header("Visuals")]
    public Transform turretPivot;
    public Transform muzzlePoint;

    private float nextFireTime = 0f;
    private Camera mainCam;
    
    // Cache Plane để không new lại mỗi frame (tối ưu nhẹ)
    private Plane groundPlane; 

    void Start()
    {
        mainCam = Camera.main;
        groundPlane = new Plane(Vector3.up, Vector3.zero);
    }

    void Update()
    {
        // --- FIX LỖI Ở ĐÂY ---
        // 1. Kiểm tra Camera null (an toàn)
        if (mainCam == null) return;

        // 2. Lấy vị trí chuột
        Vector3 mousePos = Input.mousePosition;

        // 3. QUAN TRỌNG: Kiểm tra xem vị trí chuột có bị "Infinity" hay không
        // Lỗi "screen pos inf" xảy ra do chuột chưa khởi tạo xong hoặc nằm ngoài vùng render hợp lệ
        if (float.IsInfinity(mousePos.x) || float.IsInfinity(mousePos.y)) return;
        // ---------------------

        // 1. XOAY SÚNG THEO CHUỘT
        Ray ray = mainCam.ScreenPointToRay(mousePos); // Dùng biến mousePos đã check
        
        Vector3 targetPoint = Vector3.zero;

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            targetPoint = hitPoint;

            Vector3 dir3D = hitPoint - turretPivot.position;
            dir3D.y = 0; 

            if (dir3D != Vector3.zero)
            {
                turretPivot.rotation = Quaternion.LookRotation(dir3D);
            }
        }

        // 2. BẮN SÚNG
        // Thêm kiểm tra: Chỉ bắn nếu chuột nhấn VÀ không click vào UI (nếu cần)
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Shoot(targetPoint);
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot(Vector3 targetPos)
    {
        if (bulletSystem == null) return;

        // Logic chuyển đổi tọa độ của bạn đang là: Game 3D Top-Down -> Logic đạn 2D (X, Z)
        // Vector2 startPos lấy X và Z (từ transform.position)
        Vector2 startPos = new Vector2(muzzlePoint.position.x, muzzlePoint.position.z);
        
        // Hướng bắn: targetPos.z - startPos.y (Vì startPos.y ở đây chứa giá trị Z của muzzle)
        // Logic này đúng cho game top-down thuần
        Vector2 dir = new Vector2(targetPos.x - startPos.x, targetPos.z - startPos.y).normalized;

        dir.x += UnityEngine.Random.Range(-spread, spread);
        dir.y += UnityEngine.Random.Range(-spread, spread);
        dir = dir.normalized;

        bulletSystem.SpawnBullet(startPos, dir, bulletSpeed);
    }
}