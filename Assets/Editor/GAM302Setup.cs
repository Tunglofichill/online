#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System;

/// <summary>
/// Unity Editor Auto-Setup cho GAM302 FPS Multiplayer.
/// Vào menu bar: GAM302 → 🚀 Setup Scene Tự Động
/// </summary>
public static class GAM302Setup
{
    private const string PREFAB_PATH = "Assets/Resources/Capsule.prefab";

    [MenuItem("GAM302/🚀 Setup Scene Tự Động")]
    public static void SetupAll()
    {
        bool ok = EditorUtility.DisplayDialog("GAM302 Auto Setup",
            "Script sẽ tự động tạo:\n" +
            "• Cập nhật Capsule.prefab (NCC, NetworkHitbox, CameraHolder)\n" +
            "• NetworkManager, GameManager, ChatManager\n" +
            "• Canvas: HUD, Chat, Lobby, Scoreboard\n" +
            "• Gán tất cả references\n\nTiếp tục?",
            "Bắt đầu!", "Huỷ");
        if (!ok) return;

        SetupCapsulePrefab();

        var nmGO   = EnsureGO("NetworkManager");
        var gmGO   = EnsureGO("GameManager");
        var chatGO = EnsureGO("ChatManager");

        SetupNetworkManager(nmGO);
        SetupGameManager(gmGO);
        SetupChatManager(chatGO);

        SetupCanvas(nmGO, gmGO, chatGO);
        SetupExamAssets();

        UnityEditor.SceneManagement.EditorSceneManager
            .MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("✅ Hoàn Tất!",
            "Setup xong!\n\n" +
            "⚠️  CÒN 1 BƯỚC THỦ CÔNG:\n" +
            "Chọn 'NetworkManager' trong Hierarchy\n" +
            "→ Kéo Capsule.prefab vào field 'Player Prefab'\n\n" +
            "Sau đó nhấn PLAY để test!",
            "OK – Tôi hiểu");
    }

    // ══════════════════════ CAPSULE PREFAB ══════════════════════════════════

    static void SetupCapsulePrefab()
    {
        using var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH);
        var root = scope.prefabContentsRoot;

        // NetworkCharacterController – chỉnh thông số
        var ncc = root.GetComponent<NetworkCharacterController>();
        if (ncc != null)
        {
            var so = new SerializedObject(ncc);
            SetFloat(so, "gravity",      -20f);
            SetFloat(so, "jumpImpulse",    8f);
            SetFloat(so, "maxSpeed",       6f);
            SetFloat(so, "acceleration",  10f);
            SetFloat(so, "braking",       10f);
            so.ApplyModifiedProperties();
        }

        // NetworkHitbox – thêm qua reflection (tránh lỗi assembly chưa được reference)
        TryAddComponentByName(root, "Fusion.NetworkHitbox");

        // CameraHolder child
        var camHolder = root.transform.Find("CameraHolder");
        if (camHolder == null)
        {
            var chGO = new GameObject("CameraHolder");
            chGO.transform.SetParent(root.transform, false);
            chGO.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            camHolder = chGO.transform;
        }

        // PlayerController
        var pc = root.GetComponent<PlayerController>();
        if (pc == null) pc = root.AddComponent<PlayerController>();

        var soPC = new SerializedObject(pc);
        SetFloat(soPC, "yawSensitivity",   2f);
        SetFloat(soPC, "pitchSensitivity", 2f);
        SetInt  (soPC, "damage",           25);
        SetFloat(soPC, "range",           100f);
        SetFloat(soPC, "fireRate",         0.15f);
        SetFloat(soPC, "tracerDuration",   0.06f);
        soPC.FindProperty("cameraHolder").objectReferenceValue = camHolder;
        soPC.ApplyModifiedProperties();

        Debug.Log("[GAM302] ✅ Capsule.prefab cập nhật xong");
    }

    // ══════════════════════ SCENE OBJECTS ════════════════════════════════════

    static void SetupNetworkManager(GameObject go)
    {
        var nm = GetOrAdd<NetworkManager>(go);
        var so = new SerializedObject(nm);
        so.FindProperty("autoStartOnBegin").boolValue  = true;
        so.FindProperty("defaultRoomName").stringValue = "RoomTest";
        so.ApplyModifiedProperties();
        Debug.Log("[GAM302] ✅ NetworkManager setup xong");
    }

    static void SetupGameManager(GameObject go)
    {
        var gm = GetOrAdd<GameManager>(go);
        var so = new SerializedObject(gm);
        SetInt(so, "killLimit", 5);
        so.ApplyModifiedProperties();
        Debug.Log("[GAM302] ✅ GameManager setup xong");
    }

    static void SetupChatManager(GameObject go)
    {
        GetOrAdd<ChatManager>(go);
        Debug.Log("[GAM302] ✅ ChatManager setup xong");
    }

    // ══════════════════════ CANVAS ════════════════════════════════════════════

    static void SetupCanvas(GameObject nmGO, GameObject gmGO, GameObject chatGO)
    {
        // Canvas gốc
        var canvasGO = EnsureGO("MainCanvas");
        var canvas   = GetOrAdd<Canvas>(canvasGO);
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = GetOrAdd<CanvasScaler>(canvasGO);
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        GetOrAdd<GraphicRaycaster>(canvasGO);

        // EventSystem
        if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = EnsureGO("EventSystem");
            GetOrAdd<UnityEngine.EventSystems.EventSystem>(es);
            GetOrAdd<UnityEngine.EventSystems.StandaloneInputModule>(es);
        }

        // ── HUD Panel ────────────────────────────────────────────────────────
        var hudPanel = MakePanel(canvasGO, "HUDPanel", Color.clear, stretch: true);
        hudPanel.SetActive(false); // ẩn – PlayerController.Spawned() sẽ bật lên

        var hpGO     = MakeTMP(hudPanel, "HP_Text",     "❤  HP: 100",  botLeft:true, apx:20,  apy:20,  w:260, h:40);
        var killsGO  = MakeTMP(hudPanel, "Kills_Text",  "🎯  Kills: 0", botLeft:true, apx:20,  apy:68,  w:260, h:40);
        var timerGO  = MakeTMP(hudPanel, "Timer_Text",  "⏱  05:00",    topRight:true, apx:-20, apy:-20, w:200, h:40);
        var crossGO  = MakeTMP(hudPanel, "Crosshair_Text", "+",          center:true,   apx:0,   apy:0,   w:60,  h:60, fontSize:32);
        crossGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var hud = GetOrAdd<HUDManager>(hudPanel);
        var soH = new SerializedObject(hud);
        soH.FindProperty("hpText")       .objectReferenceValue = hpGO   .GetComponent<TextMeshProUGUI>();
        soH.FindProperty("killsText")    .objectReferenceValue = killsGO .GetComponent<TextMeshProUGUI>();
        soH.FindProperty("timerText")    .objectReferenceValue = timerGO .GetComponent<TextMeshProUGUI>();
        soH.FindProperty("crosshairText").objectReferenceValue = crossGO .GetComponent<TextMeshProUGUI>();
        SetFloat(soH, "gameDuration", 300f);
        soH.ApplyModifiedProperties();

        // ── Scoreboard ───────────────────────────────────────────────────────
        var sbGO = MakeTMP(canvasGO, "Scoreboard_Text", "", center:true, apx:0, apy:50, w:500, h:600, fontSize:16);
        sbGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
        sbGO.SetActive(false);

        // ── End Game Panel ────────────────────────────────────────────────────
        var endPanel = MakePanel(canvasGO, "EndGamePanel", new Color(0,0,0,0.85f), stretch:true);
        endPanel.SetActive(false);
        var endTextGO = MakeTMP(endPanel, "EndGame_Text", "🏆  Player X Thắng!", center:true, apx:0, apy:0, w:700, h:120, fontSize:42);
        endTextGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Gán vào GameManager
        var gm   = gmGO.GetComponent<GameManager>();
        var soGm = new SerializedObject(gm);
        soGm.FindProperty("scoreboardText").objectReferenceValue = sbGO.GetComponent<TextMeshProUGUI>();
        soGm.FindProperty("endGamePanel")  .objectReferenceValue = endPanel;
        soGm.FindProperty("endGameText")   .objectReferenceValue = endTextGO.GetComponent<TextMeshProUGUI>();
        soGm.ApplyModifiedProperties();

        // ── Toast Panel ───────────────────────────────────────────────────────
        var toastPanel = MakePanel(canvasGO, "ToastPanel", new Color(0,0,0,0.7f));
        var toastRT = toastPanel.GetComponent<RectTransform>();
        toastRT.anchorMin = toastRT.anchorMax = toastRT.pivot = new Vector2(0.5f, 1f);
        toastRT.anchoredPosition = new Vector2(0, -50f);
        toastRT.sizeDelta = new Vector2(500f, 60f);
        toastPanel.SetActive(false);
        var toastTextGO = MakeTMP(toastPanel, "Toast_Text", "A player has left the match", center:true, apx:0, apy:0, w:480, h:50, fontSize:24);
        toastTextGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        
        soGm.Update(); // refresh serialized object
        soGm.FindProperty("toastPanel").objectReferenceValue = toastPanel;
        soGm.FindProperty("toastText").objectReferenceValue = toastTextGO.GetComponent<TextMeshProUGUI>();
        soGm.ApplyModifiedProperties();

        // ── Chat Panel ────────────────────────────────────────────────────────
        var chatPanel = MakePanel(canvasGO, "ChatPanel", new Color(0,0,0,0.5f));
        var chatRT    = chatPanel.GetComponent<RectTransform>();
        chatRT.anchorMin = chatRT.anchorMax = chatRT.pivot = new Vector2(0f, 0f);
        chatRT.anchoredPosition = new Vector2(10f, 180f);
        chatRT.sizeDelta        = new Vector2(350f, 200f);

        var chatDisplayGO = MakeTMP(chatPanel, "ChatDisplay", "", false, false, false, 0, 40, 340, 155, 13);
        chatDisplayGO.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        chatDisplayGO.GetComponent<RectTransform>().anchorMax = Vector2.one;
        chatDisplayGO.GetComponent<RectTransform>().offsetMin = new Vector2(5, 40);
        chatDisplayGO.GetComponent<RectTransform>().offsetMax = new Vector2(-5, -5);

        var chatInputGO = MakeInputField(chatPanel, "ChatInput", "Nhập chat... (Enter gửi)", 0, 5, 340, 35, anchor:new Vector2(0,0));

        var chat   = chatGO.GetComponent<ChatManager>();
        if (chat != null)
        {
            var soC = new SerializedObject(chat);
            soC.FindProperty("chatText")  .objectReferenceValue = chatDisplayGO.GetComponent<TextMeshProUGUI>();
            soC.FindProperty("inputField").objectReferenceValue = chatInputGO  .GetComponent<TMP_InputField>();
            soC.ApplyModifiedProperties();
        }

        // ── Lobby Panel ───────────────────────────────────────────────────────
        var lobbyPanel = MakePanel(canvasGO, "LobbyPanel", new Color(0.08f, 0.08f, 0.18f, 0.97f), stretch:true);

        MakeTMP(lobbyPanel, "LobbyTitle", "🎮  GAM302 FPS MULTIPLAYER", center:true, apx:0, apy:150, w:800, h:80, fontSize:44);

        var roomInputGO = MakeInputField(lobbyPanel, "RoomName_Input", "Tên phòng (VD: RoomTest)", 0, 30, 420, 55, anchor:new Vector2(0.5f, 0.5f));
        roomInputGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);

        var createBtnGO = MakeButton(lobbyPanel, "CreateRoom_Btn", "Tạo Phòng",  new Vector2(-120,-55), new Vector2(200, 55));
        var joinBtnGO   = MakeButton(lobbyPanel, "JoinRoom_Btn",   "Tham Gia",   new Vector2( 120,-55), new Vector2(200, 55));

        var statusGO = MakeTMP(lobbyPanel, "Status_Text", "Nhập tên phòng và chọn Tạo hoặc Tham Gia.",
            center:true, apx:0, apy:-130, w:700, h:40, fontSize:16);
        statusGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var lobby   = GetOrAdd<LobbyManager>(lobbyPanel);
        var soLobby = new SerializedObject(lobby);
        soLobby.FindProperty("networkManager").objectReferenceValue = nmGO.GetComponent<NetworkManager>();
        soLobby.FindProperty("roomNameInput") .objectReferenceValue = roomInputGO.GetComponent<TMP_InputField>();
        soLobby.FindProperty("createRoomBtn") .objectReferenceValue = createBtnGO.GetComponent<Button>();
        soLobby.FindProperty("joinRoomBtn")   .objectReferenceValue = joinBtnGO  .GetComponent<Button>();
        soLobby.FindProperty("statusText")    .objectReferenceValue = statusGO   .GetComponent<TextMeshProUGUI>();
        soLobby.FindProperty("lobbyPanel")    .objectReferenceValue = lobbyPanel;
        soLobby.FindProperty("useSameScene")  .boolValue            = true;
        soLobby.ApplyModifiedProperties();

        Debug.Log("[GAM302] ✅ Canvas setup xong");
    }

    // ══════════════════════ EXAM ASSETS SETUP ════════════════════════════════

    static void SetupExamAssets()
    {
        // Invisibility Cloak
        var cloakGO = EnsureGO("InvisibilityCloak");
        cloakGO.transform.position = new Vector3(5, 1f, 5f);
        var cloakFilter = GetOrAdd<MeshFilter>(cloakGO);
        var cloakRenderer = GetOrAdd<MeshRenderer>(cloakGO);
        var cloakCol = GetOrAdd<SphereCollider>(cloakGO);
        
        var primitiveSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cloakFilter.sharedMesh = primitiveSphere.GetComponent<MeshFilter>().sharedMesh;
        cloakRenderer.sharedMaterial = new Material(Shader.Find("Standard")) { color = Color.magenta };
        UnityEngine.Object.DestroyImmediate(primitiveSphere);
        
        cloakCol.isTrigger = true;
        cloakCol.radius = 1.5f;
        
        TryAddComponentByName(cloakGO, "Fusion.NetworkObject");
        GetOrAdd<InvisibilityCloak>(cloakGO);
        
        Debug.Log("[GAM302] ✅ Setup Moving Platform và Invisibility Cloak xong");
    }

    // ══════════════════════ UI FACTORY HELPERS ════════════════════════════════

    static GameObject MakePanel(GameObject parent, string name, Color color, bool stretch = false)
    {
        var go  = EnsureChild(parent, name);
        var rt  = GetOrAdd<RectTransform>(go);
        var img = GetOrAdd<Image>(go);
        img.color = color;
        if (stretch)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        return go;
    }

    static GameObject MakeTMP(GameObject parent, string name, string text,
        bool botLeft = false, bool topRight = false, bool center = false,
        float apx = 0, float apy = 0, float w = 200, float h = 40, int fontSize = 18)
    {
        var go  = EnsureChild(parent, name);
        var rt  = GetOrAdd<RectTransform>(go);
        var tmp = GetOrAdd<TextMeshProUGUI>(go);
        tmp.text     = text;
        tmp.fontSize = fontSize;
        tmp.color    = Color.white;

        Vector2 anchor = Vector2.zero;
        if (botLeft)  anchor = new Vector2(0f,   0f);
        if (topRight) anchor = new Vector2(1f,   1f);
        if (center)   anchor = new Vector2(0.5f, 0.5f);

        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = anchor;
        rt.anchoredPosition = new Vector2(apx, apy);
        rt.sizeDelta        = new Vector2(w,   h);
        return go;
    }

    static GameObject MakeInputField(GameObject parent, string name, string placeholder,
        float apx, float apy, float w, float h, Vector2? anchor = null)
    {
        var go  = EnsureChild(parent, name);
        var rt  = GetOrAdd<RectTransform>(go);
        var img = GetOrAdd<Image>(go);
        img.color = new Color(0.18f, 0.18f, 0.28f, 1f);

        Vector2 anc = anchor ?? new Vector2(0f, 0f);
        rt.anchorMin        = anc;
        rt.anchorMax        = anc;
        rt.pivot            = anc;
        rt.anchoredPosition = new Vector2(apx, apy);
        rt.sizeDelta        = new Vector2(w, h);

        // Text child
        var textGO = EnsureChild(go, "Text");
        StretchRT(textGO, 8, 2, -8, -2);
        var textComp = GetOrAdd<TextMeshProUGUI>(textGO);
        textComp.fontSize = 16;
        textComp.color    = Color.white;

        // Placeholder child
        var phGO = EnsureChild(go, "Placeholder");
        StretchRT(phGO, 8, 2, -8, -2);
        var phComp = GetOrAdd<TextMeshProUGUI>(phGO);
        phComp.text      = placeholder;
        phComp.fontSize  = 16;
        phComp.color     = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        phComp.fontStyle = FontStyles.Italic;

        var input             = GetOrAdd<TMP_InputField>(go);
        input.textComponent   = textComp;
        input.placeholder     = phComp;

        return go;
    }

    static GameObject MakeButton(GameObject parent, string name, string label,
        Vector2 anchoredPos, Vector2 size)
    {
        var go  = EnsureChild(parent, name);
        var rt  = GetOrAdd<RectTransform>(go);
        var img = GetOrAdd<Image>(go);
        img.color = new Color(0.18f, 0.45f, 0.88f, 1f);

        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;

        var btn = GetOrAdd<Button>(go);
        var cb  = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.6f, 1f);
        btn.colors          = cb;

        var lblGO = EnsureChild(go, "Label");
        StretchRT(lblGO, 0, 0, 0, 0);
        var lbl = GetOrAdd<TextMeshProUGUI>(lblGO);
        lbl.text      = label;
        lbl.fontSize  = 20;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color     = Color.white;

        return go;
    }

    // ══════════════════════ COMMON HELPERS ═══════════════════════════════════

    static GameObject EnsureGO(string name)
    {
        var found = GameObject.Find(name);
        return found != null ? found : new GameObject(name);
    }

    static GameObject EnsureChild(GameObject parent, string name)
    {
        var t = parent.transform.Find(name);
        if (t != null) return t.gameObject;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    static void StretchRT(GameObject go, float l, float b, float r, float t)
    {
        var rt     = GetOrAdd<RectTransform>(go);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(r, t);
    }

    static void SetFloat(SerializedObject so, string prop, float val)
        => so.FindProperty(prop).floatValue = val;

    static void SetInt(SerializedObject so, string prop, int val)
        => so.FindProperty(prop).intValue = val;

    /// <summary>Thêm component bằng tên đầy đủ (kể cả namespace) qua reflection.</summary>
    static void TryAddComponentByName(GameObject go, string fullTypeName)
    {
        var type = Type.GetType(fullTypeName);
        // Thử tìm trong tất cả assembly đã load
        if (type == null)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(fullTypeName);
                if (type != null) break;
            }
        }
        if (type == null)
        {
            Debug.LogWarning($"[GAM302] Không tìm thấy type '{fullTypeName}'. Bỏ qua.");
            return;
        }
        if (go.GetComponent(type) == null)
            go.AddComponent(type);
    }
}
#endif
