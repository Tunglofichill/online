using UnityEngine;
using Fusion;

/// <summary>
/// WeaponController – bắn súng Hitscan với Lag Compensation (Fusion 2).
/// ─── SETUP ───────────────────────────────────────────────────────────────────
/// 1. Gắn vào cùng GameObject với PlayerController.
/// 2. Tạo child Transform tên "MuzzlePoint" ở đầu nòng súng → gán vào muzzlePoint.
/// 3. hitMask: chọn Layer "Player" để tránh bắn trúng chính mình.
/// 4. Thêm NetworkHitbox lên Capsule prefab để Lag Compensation hoạt động tốt nhất.
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public class WeaponController : NetworkBehaviour
{
    [Header("Stats")]
    public int   damage   = 25;
    public float range    = 100f;
    public float fireRate = 0.15f;   // Giây giữa 2 phát bắn

    [Header("References")]
    [Tooltip("Transform ở đầu nòng súng dùng để spawn hiệu ứng.")]
    public Transform muzzlePoint;

    [Tooltip("Layer nào có thể bị bắn trúng (nên exclude layer của chính mình).")]
    public LayerMask hitMask = ~0;

    // ── Private ─────────────────────────────────────────────────────────────
    private float      _nextFireTime = 0f;
    private BulletPool _pool;

    void Awake()
    {
        _pool = FindObjectOfType<BulletPool>();
    }

    /// <summary>
    /// Gọi từ PlayerController.FixedUpdateNetwork() khi input.Fire == true.
    /// Chỉ client có InputAuthority mới xử lý bắn – kết quả gửi lên StateAuthority của target.
    /// </summary>
    public void Shoot()
    {
        if (!HasInputAuthority) return;
        if (Runner.SimulationTime < _nextFireTime) return;

        _nextFireTime = Runner.SimulationTime + fireRate;

        // ── Tính hướng bắn từ giữa màn hình (FPS style) ──
        Camera cam    = Camera.main;
        Vector3 origin = cam != null ? cam.transform.position
                                     : (muzzlePoint != null ? muzzlePoint.position : transform.position);
        Vector3 dir    = cam != null ? cam.transform.forward : transform.forward;

        // ── Lag Compensation Raycast ──
        // Fusion 2 cung cấp Runner.LagCompensation.Raycast() để bù trễ mạng.
        // Yêu cầu: NetworkHitbox component trên từng Player prefab.
        // Phiên bản này dùng LagCompensation cho client phán đoán tại chỗ (Shared Mode):
        var lagOptions = HitOptions.SubtickAccuracy | HitOptions.IncludePhysX;

        if (Runner.LagCompensation.Raycast(
                origin, dir, range,
                player: Runner.LocalPlayer,
                hit:    out LagCompensatedHit lcHit,
                layerMask: hitMask,
                options: lagOptions))
        {
            var target = lcHit.GameObject?.GetComponent<PlayerController>();
            if (target != null && target.Object != null
                && target.Object.InputAuthority != Runner.LocalPlayer
                && !target.IsDead)
            {
                // Gửi RPC sát thương lên StateAuthority của mục tiêu
                target.RPC_TakeDamage(damage, Runner.LocalPlayer);
                Debug.Log($"[Weapon] 💥 Hit P{target.Object.InputAuthority.PlayerId} – {damage} dmg");
            }
        }
        else if (Physics.Raycast(origin, dir, out RaycastHit phxHit, range, hitMask))
        {
            // Fallback: Physics.Raycast nếu không có NetworkHitbox
            var target = phxHit.collider.GetComponent<PlayerController>();
            if (target != null && target.Object != null
                && target.Object.InputAuthority != Runner.LocalPlayer
                && !target.IsDead)
            {
                target.RPC_TakeDamage(damage, Runner.LocalPlayer);
            }
        }

        // ── Visual effect (Object Pool) ──
        _pool?.SpawnBulletEffect(origin, dir);
    }
}
