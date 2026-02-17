using UnityEngine;
using VContainer; // Dùng VContainer để lấy BulletSystem

public class GunController : MonoBehaviour
{
    [Inject] private BulletSystem bulletSystem; // <--- Dependency Injection

    [Header("Gun Stats")]
    public float fireRate = 0.1f; // Tốc độ bắn (giây)
    public float bulletSpeed = 20f;
    public float spread = 0.1f;   // Độ giật (tản mát)
    
    [Header("Visuals")]
    public Transform turretPivot; // Cái trục xoay của súng
    public Transform muzzlePoint; // Đầu nòng súng (nơi đạn chui ra)

    private float nextFireTime = 0f;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        // 1. XOAY SÚNG THEO CHUỘT
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        
        Vector3 targetPoint = Vector3.zero;

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            targetPoint = hitPoint;

            // Tính hướng từ súng tới chuột
            Vector3 dir3D = hitPoint - turretPivot.position;
            dir3D.y = 0; // Chỉ xoay quanh trục Y

            if (dir3D != Vector3.zero)
            {
                turretPivot.rotation = Quaternion.LookRotation(dir3D);
            }
        }

        // 2. BẮN SÚNG (Giữ chuột trái)
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Shoot(targetPoint);
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot(Vector3 targetPos)
    {
        if (bulletSystem == null) return;

        // Tính hướng bắn
        Vector2 startPos = new Vector2(muzzlePoint.position.x, muzzlePoint.position.z);
        
        // Hướng chuẩn
        Vector2 dir = new Vector2(targetPos.x - startPos.x, targetPos.z - startPos.y).normalized;

        // Thêm độ tản mát (Random Spread) giả lập súng máy rung
        dir.x += UnityEngine.Random.Range(-spread, spread);
        dir.y += UnityEngine.Random.Range(-spread, spread);
        dir = dir.normalized;

        // GỌI SYSTEM ĐỂ SPAWN
        bulletSystem.SpawnBullet(startPos, dir, bulletSpeed);
    }
}