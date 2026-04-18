using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NetworkManager – Quản lý kết nối Photon Fusion 2 (Shared Mode).
/// Thu thập input mỗi frame, gửi qua OnInput callback của Fusion.
/// ─── SETUP ───────────────────────────────────────────────────────────────────
/// 1. Thêm NetworkManager component vào một GameObject trong Scene.
/// 2. Gán playerPrefab = NetworkPrefabRef trỏ đến Player Prefab (phải có NetworkObject).
/// 3. Nếu dùng LobbyManager: LobbyManager sẽ gọi StartAsShared(roomName).
///    Nếu không: Set autoStartOnBegin = true để tự join room mặc định.
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static NetworkManager Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Network")]
    public NetworkPrefabRef playerPrefab;

    [Header("Settings")]
    [Tooltip("Nếu true: tự động join room mặc định khi game bắt đầu (bỏ qua Lobby).")]
    public bool   autoStartOnBegin = false;
    [Tooltip("Tên phòng mặc định khi autoStartOnBegin = true.")]
    public string defaultRoomName  = "RoomTest";

    // ── Input accumulation (polled mỗi Update frame, consumed trong OnInput tick) ──
    private float _accumRotationY = 0f;   // Tích lũy mouse X → góc tuyệt đối của body
    private bool  _jumpQueued     = false; // one-shot

    // ── Runtime ───────────────────────────────────────────────────────────────
    private NetworkRunner _runner;
    private bool          _spawnedLocalPlayer = false;

    // ═════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Nếu dùng PlayerPrefs từ LobbyScene riêng
        string savedRoom = PlayerPrefs.GetString("RoomName", "");
        if (!string.IsNullOrEmpty(savedRoom))
        {
            PlayerPrefs.DeleteKey("RoomName");
            StartAsShared(savedRoom);
            return;
        }

        if (autoStartOnBegin && FindObjectOfType<LobbyManager>() == null)
            StartAsShared(defaultRoomName);
        else if (autoStartOnBegin)
        {
            Debug.Log("[Network] Bỏ qua AutoStartOnBegin vì bạn đang sử dụng LobbyManager.");
        }
    }

    void Update()
    {
        // Thu thập input mỗi frame – tích lũy để không mất event giữa 2 Fusion tick
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            _accumRotationY += Input.GetAxisRaw("Mouse X") * 2f;
        }
        if (Input.GetButtonDown("Jump")) _jumpQueued = true;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PUBLIC API
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Kết nối vào Fusion session. Gọi từ LobbyManager.</summary>
    public async void StartAsShared(string roomName)
    {
        if (_runner != null)
        {
            Debug.LogWarning("[Network] Đã có runner đang chạy!");
            return;
        }

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        Debug.Log($"<color=cyan>[Network]</color> Đang join room: <color=yellow>{roomName}</color>");

        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode     = GameMode.Shared,
            SessionName  = roomName,
            Scene        = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
            Debug.Log($"<color=green>[Network]</color> Đã vào phòng '{roomName}' thành công!");
        else
            Debug.LogError($"[Network] Lỗi kết nối: {result.ShutdownReason}");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // FUSION CALLBACKS
    // ═════════════════════════════════════════════════════════════════════════

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new PlayerNetworkInput
        {
            Move       = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            RotationY  = _accumRotationY,             // Góc tuyệt đối (tích lũy)
            Jump       = _jumpQueued,                 // One-shot
            Fire       = Input.GetButton("Fire1"),    // Hold-to-fire
        };
        input.Set(data);

        // Chỉ reset one-shot, KHÔNG reset _accumRotationY (nó là góc tuyệt đối)
        _jumpQueued = false;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"<color=cyan>[Network]</color> P{player.PlayerId} đã vào phòng.");

        // Chỉ spawn player local của mình (mỗi client tự spawn player của mình)
        if (player == runner.LocalPlayer && !_spawnedLocalPlayer)
        {
            _spawnedLocalPlayer = true;
            Vector3 spawnPos = new Vector3(
                Random.Range(-5f, 5f), 1.5f, Random.Range(-5f, 5f)
            );
            runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
            Debug.Log($"<color=green>[Network]</color> Spawned local player P{player.PlayerId}");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"<color=orange>[Network]</color> P{player.PlayerId} đã rời phòng.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowPlayerLeftPopup();
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[Network] Shutdown: {shutdownReason}");
        _runner = null;
        _spawnedLocalPlayer = false;
    }

    // ── Unused callbacks (bắt buộc implement INetworkRunnerCallbacks) ─────────
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}