using Fusion;
using UnityEngine;

/// <summary>
/// Struct truyền input từ client lên tất cả peers qua Photon Fusion Shared Mode.
/// RotationY là góc Euler tuyệt đối (tích lũy từ mouse X) để đồng bộ hướng nhân vật.
/// </summary>
public struct PlayerNetworkInput : INetworkInput
{
    public Vector2 Move;        // WASD → Vector2
    public float   RotationY;  // Góc xoay ngang (Euler Y) tích lũy – gửi tuyệt đối
    public bool    Jump;       // One-shot: nhảy
    public bool    Fire;       // Hold: bắn liên tục
}
