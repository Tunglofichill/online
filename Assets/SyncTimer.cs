using UnityEngine;
using TMPro;
using Fusion;

public class SyncTimer : NetworkBehaviour
{
    [Header("UI")]
    public TMP_Text timerDisplay;

    // Lưu thời gian của từng player
    [Networked]
    public NetworkDictionary<int, float> PlayerTimes { get; } = new NetworkDictionary<int, float>();

    private float startTime = 0f;

    public override void Spawned()
    {
        startTime = Runner.SimulationTime;
        Debug.Log($"[SyncTimer] Timer started on P{Runner.LocalPlayer.PlayerId}");
    }

    public override void FixedUpdateNetwork()
    {
        // Mọi client đều cập nhật thời gian của chính mình
        int myId = Runner.LocalPlayer.PlayerId;
        float elapsed = Runner.SimulationTime - startTime;

        // Cập nhật thời gian
        if (!PlayerTimes.ContainsKey(myId) || Mathf.Abs(PlayerTimes[myId] - elapsed) > 0.1f)
        {
            PlayerTimes.Set(myId, elapsed);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ShowAllTimes();
        }
    }

    private void ShowAllTimes()
    {
        if (timerDisplay == null) return;

        string text = "<b>Thời gian đồng bộ (Tab):</b>\n\n";

        if (PlayerTimes.Count == 0)
        {
            text += "Đang chờ dữ liệu...\n";
        }
        else
        {
            foreach (var item in PlayerTimes)
            {
                text += $"P{item.Key}: {item.Value:F2}s\n";
            }
        }

        text += $"\nLocal Player: P{Runner.LocalPlayer.PlayerId}";

        timerDisplay.text = text;
        timerDisplay.gameObject.SetActive(true);

        // Tự ẩn sau 5 giây
        CancelInvoke("HideTimer");
        Invoke("HideTimer", 5f);
    }

    private void HideTimer()
    {
        if (timerDisplay != null)
            timerDisplay.gameObject.SetActive(false);
    }
}