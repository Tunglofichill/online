# GAM302 – Hướng dẫn Setup Game trong Unity

---

## Scripts cần dùng (những cái còn lại bỏ qua hoặc xóa)

| Script | Gắn vào đâu | Chức năng |
|---|---|---|
| `PlayerNetworkInput.cs` | (struct, không gắn vào đâu) | Truyền input qua Fusion |
| `PlayerController.cs` | Capsule Prefab | Di chuyển, nhảy, bắn, HP, Kills, Camera |
| `NetworkManager.cs` | Scene Empty GO | Kết nối Fusion, spawn player |
| `LobbyManager.cs` | Canvas LobbyPanel | UI Tạo/Tham gia phòng |
| `HUDManager.cs` | Canvas HUDPanel | HP, Kills, Timer |
| `GameManager.cs` | Scene Empty GO | Kill Limit, Scoreboard, End Game |
| `ChatManager.cs` | Scene Empty GO | Chat RPC đồng bộ |

**Xóa hoặc bỏ qua:** `WeaponController.cs`, `BulletPool.cs`, `PlayFabManager.cs`, `PlayerSpawn.cs`

---

## BƯỚC 1 – Setup Player Prefab (Capsule)

Vào `Assets/Resources/` → mở **Capsule.prefab**

### 1.1 – Components phải có trên root Capsule:

| Component | Ghi chú |
|---|---|
| **CharacterController** | Thường đã có sẵn |
| **NetworkObject** | Thường đã có sẵn |
| **NetworkCharacterController** | Chỉnh thông số như bên dưới |
| **NetworkHitbox** | Add thêm – cần để Lag Compensation bắn trúng chính xác |
| **PlayerController** | Script chính – mọi thứ đều ở đây |

### 1.2 – Thông số NetworkCharacterController (Inspector):

```
gravity       = -20
jumpImpulse   =   8
maxSpeed      =   6
acceleration  =  10
braking       =  10
```

### 1.3 – Thông số PlayerController (Inspector):

```
Yaw Sensitivity    = 2       (độ nhạy chuột ngang)
Pitch Sensitivity  = 2       (độ nhạy chuột dọc)
Camera Holder      = [kéo CameraHolder vào đây]
Damage             = 25
Range              = 100
Fire Rate          = 0.15
Hit Mask           = Everything (mặc định ~0)
Tracer Color       = vàng (mặc định)
Tracer Duration    = 0.06
```

### 1.4 – Tạo Child Object "CameraHolder" trong Prefab:

1. Mở Capsule.prefab
2. Chuột phải vào Capsule root → **Create Empty** → đặt tên `CameraHolder`
3. Set Local Position: `X=0, Y=0.7, Z=0`

```
Capsule (root)
└── CameraHolder   ← Local Position Y = 0.7
```

4. Kéo `CameraHolder` vào field **Camera Holder** của PlayerController

---

## BƯỚC 2 – Setup Scene (SampleScene)

### 2.1 – NetworkManager

1. Chuột phải trong Hierarchy → **Create Empty** → đặt tên `NetworkManager`
2. Add Component → **NetworkManager**
3. Kéo **Capsule.prefab** từ `Assets/Resources/` vào field **Player Prefab**
4. Tick ✅ **Auto Start On Begin** = true _(tự join phòng khi chạy, không cần bấm Lobby)_
5. **Default Room Name** = `RoomTest`

### 2.2 – GameManager

1. Create Empty → đặt tên `GameManager`
2. Add Component → **GameManager**
3. **Kill Limit** = `5` _(test nhanh, đổi thành 10 khi nộp bài)_
4. Để trống các field UI trước, gán sau khi tạo Canvas

### 2.3 – ChatManager

1. Create Empty → đặt tên `ChatManager`
2. Add Component → **ChatManager**
3. Gán UI sau khi tạo Canvas

---

## BƯỚC 3 – Setup Canvas (HUD + Lobby + Chat)

**Create → UI → Canvas** → tên `MainCanvas`
- Canvas Scaler: **Scale With Screen Size**
- Reference Resolution: **1920 × 1080**

---

### 3.1 – HUD Panel

Tạo **Panel** con trong MainCanvas → tên `HUDPanel`
Thêm script **HUDManager** vào HUDPanel

Tạo các TMP_Text bên trong HUDPanel:

| Tên Object | Kiểu | Vị trí | Text mặc định |
|---|---|---|---|
| `HP_Text` | TMP_Text | Góc trái dưới | `❤ HP: 100` |
| `Kills_Text` | TMP_Text | Cạnh HP_Text | `🎯 Kills: 0` |
| `Timer_Text` | TMP_Text | Góc phải trên | `⏱ 05:00` |
| `Crosshair_Text` | TMP_Text | Chính giữa màn hình | `+` (font size 24) |

Gán vào **HUDManager** (Inspector):
- `hpText`        → HP_Text
- `killsText`     → Kills_Text
- `timerText`     → Timer_Text
- `crosshairText` → Crosshair_Text
- `gameDuration`  = `300`

> HUDPanel tự ẩn khi game chạy và tự hiện khi player spawn xong.

---

### 3.2 – Scoreboard + End Game

Tạo trực tiếp trong MainCanvas (không cần panel cha):

| Tên Object | Kiểu | Ghi chú |
|---|---|---|
| `Scoreboard_Text` | TMP_Text | Giữa màn, **Active = OFF** mặc định |
| `EndGamePanel` | Panel | **Active = OFF** mặc định |
| `EndGame_Text` | TMP_Text | Bên trong EndGamePanel |

Gán vào **GameManager** (Inspector):
- `scoreboardText` → Scoreboard_Text
- `endGamePanel`   → EndGamePanel
- `endGameText`    → EndGame_Text

---

### 3.3 – Chat Panel

Tạo **Panel** con → tên `ChatPanel` (nên để góc trái hoặc phải màn hình)

| Tên Object | Kiểu | Ghi chú |
|---|---|---|
| `ChatDisplay` | TMP_Text | Vùng hiển thị tin nhắn |
| `ChatInput` | TMP_InputField | Ô nhập tin nhắn |

Gán vào **ChatManager** (Inspector):
- `chatText`   → ChatDisplay
- `inputField` → ChatInput

---

### 3.4 – Lobby Panel

Tạo **Panel** con → tên `LobbyPanel`
Thêm script **LobbyManager** vào LobbyPanel

| Tên Object | Kiểu |
|---|---|
| `RoomName_Input` | TMP_InputField |
| `CreateRoom_Btn` | Button – text "Tạo Phòng" |
| `JoinRoom_Btn` | Button – text "Tham Gia" |
| `Status_Text` | TMP_Text |

Gán vào **LobbyManager** (Inspector):
- `networkManager` → GameObject **NetworkManager** trong Hierarchy
- `roomNameInput`  → RoomName_Input
- `createRoomBtn`  → CreateRoom_Btn
- `joinRoomBtn`    → JoinRoom_Btn
- `statusText`     → Status_Text
- `lobbyPanel`     → LobbyPanel _(kéo chính nó vào – sẽ tự ẩn sau khi kết nối)_
- ✅ `useSameScene` = true

> Nếu đã bật **Auto Start On Begin** trên NetworkManager thì game tự kết nối mà không cần Lobby.

---

## BƯỚC 4 – Kiểm tra Build Settings

**File → Build Settings** → đảm bảo **SampleScene** có trong danh sách, index = **0**

---

## BƯỚC 5 – Test Multiplayer (2 client trên 1 máy)

1. **File → Build Settings → Build** → chọn thư mục `build/`
2. Chạy file `.exe` vừa build **(Client 1)**
3. Nhấn **Play** trong Unity Editor **(Client 2)**
4. Cả 2 tự join phòng `"RoomTest"` → thấy nhau và bắn được nhau

---

## Controls

| Phím / Chuột | Hành động |
|---|---|
| `WASD` | Di chuyển |
| `Space` | Nhảy |
| `Chuột` | Xoay camera |
| `Click trái` | Bắn |
| `Tab` (giữ) | Bảng điểm Scoreboard |
| `Enter` | Gửi tin chat (khi ô chat đang focus) |

---

## BƯỚC 6 – Đóng gói nộp bài

**Xóa trước khi nén** (Unity tự tạo lại, không cần nộp):
```
Library/
Temp/
obj/
build/
```

Nén thư mục project → đặt tên: **`MSSV_Game.zip`** (phải ≤ 50MB)

---

## Tổng kết yêu cầu đề bài

| Yêu cầu đề | Script thực hiện | Trạng thái |
|---|---|---|
| Y1: Tạo/Join phòng (UI) | LobbyManager + NetworkManager | ✅ |
| Y1: Di chuyển | PlayerController | ✅ |
| Y1: Nhảy | PlayerController | ✅ |
| Y1: Camera (Cinemachine-style) | PlayerController (gắn Main Camera) | ✅ |
| Y1: Bắn / Va chạm vật lý | PlayerController (Raycast) | ✅ |
| Y2: Chat đồng bộ | ChatManager | ✅ |
| Y2: Đồng bộ HUD – HP | PlayerController [Networked] + HUDManager | ✅ |
| Y2: Đồng bộ HUD – Kill | PlayerController [Networked] + HUDManager | ✅ |
| Y2: Đồng bộ HUD – Timer | HUDManager | ✅ |
| Y2: Object Pooling | PlayerController (LineRenderer pool nội bộ) | ✅ |
| Y2: Lag Compensation | PlayerController (LagCompensation.Raycast) | ✅ |
| Y3: PlayFab Backend | ❌ Đã bỏ | – |

**Dự kiến điểm: 7 – 8 / 10**
