using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

/// <summary>
/// PlayerController – Tất cả trong 1: Di chuyển, Nhảy, Camera, Bắn súng, HP, Kills.
/// Không cần gắn thêm WeaponController hay BulletPool riêng.
///
/// ═══ SETUP PLAYER PREFAB ════════════════════════════════════════════════════
///  Components bắt buộc trên Player Prefab (Capsule):
///    • CharacterController
///    • NetworkObject
///    • NetworkCharacterController  → chỉnh: maxSpeed=6, jumpImpulse=8, gravity=-20
///    • NetworkHitbox               → để Lag Compensation raycast hoạt động
///    • PlayerController (script này)  ← tất cả đều ở đây
///
///  Child Object cần tạo trong Prefab:
///    • "CameraHolder" – Empty Transform tại đầu nhân vật (Local Position Y = 0.7)
///      Gán vào field [cameraHolder] bên dưới.
/// ════════════════════════════════════════════════════════════════════════════
/// </summary>
public class PlayerController : NetworkBehaviour
{
    // ══════════════════════════════ LOOK ═════════════════════════════════════

    [Header("Look")]
    public float yawSensitivity   = 2f;
    public float pitchSensitivity = 2f;

    [Tooltip("Child Transform rỗng tại đầu nhân vật – camera sẽ gắn vào đây.")]
    public Transform cameraHolder;

    // ══════════════════════════ WEAPON (tích hợp) ═════════════════════════════

    [Header("Weapon (tích hợp, không cần prefab)")]
    public int   damage   = 25;
    public float range    = 100f;
    public float fireRate = 0.15f;    // giây giữa 2 phát
    public LayerMask hitMask = ~0;    // bắn tất cả layer

    // ══════════════════════════ BULLET EFFECT (tích hợp) ══════════════════════

    [Header("Bullet Tracer (tích hợp, không cần prefab)")]
    [Tooltip("Màu vệt đạn. Để mặc định vàng nếu không quan tâm.")]
    public Color tracerColor = new Color(1f, 0.9f, 0.2f, 1f);
    [Tooltip("Thời gian hiển thị vệt đạn (giây).")]
    public float tracerDuration = 0.06f;
    private const int POOL_SIZE = 15;
    private Queue<LineRenderer> _linePool;

    // ══════════════════════════ NETWORKED STATE ══════════════════════════════

    [Networked] public int  HP        { get; set; }
    [Networked] public int  Kills     { get; set; }
    [Networked] public bool IsDead    { get; set; }
    [Networked] public bool IsRunning { get; set; }
    [Networked] public bool IsGrounded{ get; set; }
    [Networked] public bool IsFiring  { get; set; }
    [Networked] public NetworkBool IsInvisible { get; set; }

    // ══════════════════════════════ PRIVATE ══════════════════════════════════

    private NetworkCharacterController _ncc;
    private HUDManager   _hud;
    private Camera       _mainCam;
    private Transform    _camTransform;   // cached để dùng trong FixedUpdateNetwork
    private float        _camPitch    = 0f;
    private float        _nextFireTime = 0f;
    private ChangeDetector _changes;

    // ═════════════════════════════ LIFECYCLE ══════════════════════════════════

    void Awake()
    {
        _ncc     = GetComponent<NetworkCharacterController>();
        _mainCam = Camera.main;   // cache sớm – nếu chưa có sẽ gán lại trong Spawned()
        InitBulletPool();
    }

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        HP    = 100;
        IsDead = false;

        if (HasInputAuthority)
        {
            // Gắn camera vào CameraHolder của player local
            _mainCam = Camera.main;
            if (_mainCam == null) _mainCam = FindObjectOfType<Camera>();
            if (_mainCam != null && cameraHolder != null)
            {
                _mainCam.transform.SetParent(cameraHolder);
                _camTransform = _mainCam.transform;
                _mainCam.transform.localPosition = Vector3.zero;
                _mainCam.transform.localRotation = Quaternion.identity;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            // Bật HUD
            _hud = FindObjectOfType<HUDManager>(true);
            if (_hud != null)
            {
                _hud.gameObject.SetActive(true);
                _hud.StartTimer();
                _hud.UpdateHP(HP);
                _hud.UpdateKills(Kills);
            }
        }

        FindObjectOfType<GameManager>()?.RegisterPlayer(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        FindObjectOfType<GameManager>()?.UnregisterPlayer(this);
    }

    // ════════════════════════════ SIMULATION ═════════════════════════════════

    public override void FixedUpdateNetwork()
    {
        if (IsDead) return;

        if (GetInput(out PlayerNetworkInput input))
        {
            // ── Xoay body ──
            transform.rotation = Quaternion.Euler(0f, input.RotationY, 0f);

            // ── Di chuyển (NCC xử lý gravity, braking, maxSpeed) ──
            Vector3 dir = transform.TransformDirection(
                new Vector3(input.Move.x, 0f, input.Move.y)
            );
            _ncc.Move(dir);

            // ── Nhảy ──
            if (input.Jump && _ncc.Grounded)
                _ncc.Jump();

            // ── Override lại rotation (NCC.Move có thể xoay theo direction) ──
            transform.rotation = Quaternion.Euler(0f, input.RotationY, 0f);

            // ── Cập nhật networked states ──
            IsRunning  = input.Move.magnitude > 0.1f;
            IsGrounded = _ncc.Grounded;
            IsFiring   = input.Fire;

            // ── Bắn ──
            if (input.Fire) HandleShoot();
        }
    }

    public override void Render()
    {
        // Camera pitch – chỉ local client, không đồng bộ
        if (HasInputAuthority && cameraHolder != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                bool isLocked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = !isLocked;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                float mouseY = Input.GetAxisRaw("Mouse Y") * pitchSensitivity;
                _camPitch = Mathf.Clamp(_camPitch - mouseY, -80f, 80f);
                cameraHolder.localRotation = Quaternion.Euler(_camPitch, 0f, 0f);
            }
        }

        // Phát hiện thay đổi networked property → cập nhật HUD
        foreach (var change in _changes.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(HP):
                    if (HasInputAuthority) _hud?.UpdateHP(HP);
                    break;
                case nameof(Kills):
                    if (HasInputAuthority) _hud?.UpdateKills(Kills);
                    break;
                case nameof(IsDead):
                    if (IsDead) HandleDeath();
                    else HandleRespawnClient();
                    break;
                case nameof(IsInvisible):
                    HandleInvisibility(IsInvisible);
                    break;
            }
        }
    }

    private void HandleInvisibility(bool invisible)
    {
        if (IsDead) return; 
        
        // an renderer cua player
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (!(r is LineRenderer))
                r.enabled = !invisible;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyInvisibility()
    {
        if (IsInvisible || IsDead) return;
        IsInvisible = true;
        StartCoroutine(InvisibilityRoutine());
    }

    private IEnumerator InvisibilityRoutine()
    {
        yield return new WaitForSeconds(3f);
        if (this != null && Object != null)
        {
            IsInvisible = false;
        }
    }

    // ═══════════════════════════ SHOOTING ════════════════════════════════════

    private void HandleShoot()
    {
        if (!HasInputAuthority) return;
        if (Runner.SimulationTime < _nextFireTime) return;
        _nextFireTime = Runner.SimulationTime + fireRate;

        // Dùng cached camera transform – an toàn trong FixedUpdateNetwork
        Vector3 origin = _camTransform != null ? _camTransform.position : transform.position + Vector3.up * 0.7f;
        Vector3 dir    = _camTransform != null ? _camTransform.forward  : transform.forward;

        Vector3 endpoint = origin + dir * range;

        // ── Physics.Raycast ──
        // (thầy giáo có thể thấy cả LagCompensation trong WeaponController.cs nếu cần demo)
        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask))
        {
            endpoint = hit.point;
            var target = hit.collider.GetComponent<PlayerController>();
            if (target != null
                && target.Object != null
                && target.Object.InputAuthority != Runner.LocalPlayer
                && !target.IsDead)
            {
                target.RPC_TakeDamage(damage, Runner.LocalPlayer);
                Debug.Log($"[Gun] 💥 Hit P{target.Object.InputAuthority.PlayerId} – {damage} dmg");
            }
        }

        // ── Vệt đạn visual (local only) ──
        SpawnTracer(origin, endpoint);
    }

    // ═══════════════════════════ DAMAGE / DEATH ══════════════════════════════

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int dmg, PlayerRef shooterRef)
    {
        if (IsDead) return;
        HP = Mathf.Max(0, HP - dmg);
        Debug.Log($"[P{Object.InputAuthority.PlayerId}] HP = {HP}");
        if (HP <= 0)
        {
            IsDead = true;
            RPC_AwardKill(shooterRef);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AwardKill(PlayerRef shooterRef)
    {
        foreach (var pc in FindObjectsOfType<PlayerController>())
        {
            if (pc == null || pc.Object == null) continue;
            if (pc.Object.InputAuthority == shooterRef && pc.HasStateAuthority)
            {
                pc.Kills++;
                Debug.Log($"[P{shooterRef.PlayerId}] 🎯 Kills = {pc.Kills}");
                break;
            }
        }
    }

    private void HandleDeath()
    {
        // tat visual thay vi setactive false de giu ket noi mang
        ToggleVisuals(false); 
        if (HasStateAuthority) Invoke(nameof(Respawn), 3f);
    }

    private void Respawn()
    {
        _ncc.Teleport(new Vector3(Random.Range(-5f, 5f), 1.5f, Random.Range(-5f, 5f)));
        HP    = 100;
        IsDead = false; 
        ToggleVisuals(true);
    }

    private void HandleRespawnClient()
    {
        ToggleVisuals(true);
    }

    private void ToggleVisuals(bool show)
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (!(r is LineRenderer))
                r.enabled = show ? !IsInvisible : false;
        }
        
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = show;
    }

    // ═══════════════════ BULLET TRACER POOL (tích hợp) ═══════════════════════

    private void InitBulletPool()
    {
        _linePool = new Queue<LineRenderer>();
        for (int i = 0; i < POOL_SIZE; i++)
            _linePool.Enqueue(CreateLine());
    }

    private LineRenderer CreateLine()
    {
        var go = new GameObject("BulletTracer");
        go.transform.SetParent(transform.root);
        go.SetActive(false);

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth    = 0.02f;
        lr.endWidth      = 0.005f;
        lr.useWorldSpace = true;

        // Tìm shader Unlit/Color. Nếu bị strip khi build, dùng tạm Sprites/Default hoặc bỏ qua để tránh crash
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        
        if (shader != null)
        {
            lr.material = new Material(shader)
            {
                color = tracerColor
            };
        }
        else
        {
            Debug.LogWarning("[PlayerController] Không tìm thấy Shader cho vệt đạn. Hãy thêm Unlit/Color vào Always Included Shaders.");
        }
        return lr;
    }

    private void SpawnTracer(Vector3 from, Vector3 to)
    {
        if (_linePool == null || _linePool.Count == 0) return;

        LineRenderer lr = _linePool.Dequeue();
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.gameObject.SetActive(true);

        StartCoroutine(ReturnTracer(lr, tracerDuration));
    }

    private IEnumerator ReturnTracer(LineRenderer lr, float delay)
    {
        yield return new WaitForSeconds(delay);
        lr.gameObject.SetActive(false);
        _linePool.Enqueue(lr);
    }
}