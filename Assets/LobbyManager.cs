using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// LobbyManager – Giao diện Lobby để tạo hoặc tham gia phòng.
/// ─── SETUP ───────────────────────────────────────────────────────────────────
/// CÁCH 1 – Dùng trực tiếp trong SampleScene (không cần scene riêng):
///   1. Thêm LobbyManager vào một GameObject trong SampleScene.
///   2. Tạo UI Canvas với:
///       • TMP_InputField → gán vào roomNameInput
///       • Button "Tạo Phòng" → gán vào createRoomBtn
///       • Button "Tham Gia"  → gán vào joinRoomBtn
///       • TMP_Text status    → gán vào statusText
///   3. Gán NetworkManager GameObject vào networkManager.
///   4. Checkbox useSameScene = true (không cần scene Lobby riêng).
///
/// CÁCH 2 – Scene Lobby riêng:
///   1. Tạo scene "LobbyScene", thêm LobbyManager + NetworkManager prefab.
///   2. Đặt gameSceneName = "SampleScene".
///   3. Thêm cả 2 scene vào Build Settings.
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public class LobbyManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("NetworkManager trong Scene (hoặc DontDestroyOnLoad).")]
    public NetworkManager networkManager;

    [Header("UI")]
    public TMP_InputField roomNameInput;
    public Button         createRoomBtn;
    public Button         joinRoomBtn;
    public TMP_Text       statusText;

    [Header("Settings")]
    [Tooltip("Nếu true: Lobby và Game dùng chung SampleScene (không cần load scene mới).")]
    public bool   useSameScene    = true;
    [Tooltip("Tên scene game (chỉ dùng khi useSameScene = false).")]
    public string gameSceneName   = "SampleScene";

    [Header("Lobby UI Panel")]
    [Tooltip("Panel chứa UI Lobby – sẽ ẩn đi sau khi kết nối.")]
    public GameObject lobbyPanel;

    void Start()
    {
        // ÉP CHẾ ĐỘ CỬA SỔ (Ngăn Unity tự động lấy Fullscreen từ cache Windows cũ)
        Screen.SetResolution(1280, 720, FullScreenMode.Windowed);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Tìm NetworkManager trong scene nếu chưa gán
        if (networkManager == null)
            networkManager = FindObjectOfType<NetworkManager>();

        if (createRoomBtn != null) createRoomBtn.onClick.AddListener(OnClickCreate);
        if (joinRoomBtn != null)   joinRoomBtn.onClick.AddListener(OnClickJoin);

        SetStatus("Nhập tên phòng và chọn Tạo hoặc Tham gia.");
    }

    // ── Button Handlers ──────────────────────────────────────────────────────

    private void OnClickCreate()
    {
        string room = GetRoomName("RoomTest"); // Ép cả Tạo và Tham Gia dùng chung phòng mặc định
        SetStatus($"⏳ Đang tạo/vào phòng '{room}'…");
        Connect(room);
    }

    private void OnClickJoin()
    {
        string room = GetRoomName("RoomTest");
        SetStatus($"⏳ Đang tham gia phòng '{room}'…");
        Connect(room);
    }

    // ── Core ─────────────────────────────────────────────────────────────────

    private void Connect(string roomName)
    {
        if (networkManager == null)
        {
            SetStatus("❌ Lỗi: Không tìm thấy NetworkManager!");
            return;
        }

        // Ẩn lobby UI
        if (lobbyPanel != null) lobbyPanel.SetActive(false);

        if (useSameScene)
        {
            // Kết nối ngay trong scene hiện tại
            networkManager.StartAsShared(roomName);
        }
        else
        {
            // Lưu tên phòng rồi load scene game
            PlayerPrefs.SetString("RoomName", roomName);
            SceneManager.LoadScene(gameSceneName);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string GetRoomName(string fallback)
    {
        return (roomNameInput != null && !string.IsNullOrWhiteSpace(roomNameInput.text))
            ? roomNameInput.text.Trim()
            : fallback;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log("[Lobby] " + msg);
    }
}
