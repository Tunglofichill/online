using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;

/// <summary>
/// GameManager – quản lý trạng thái trận đấu: Kill Limit, Scoreboard, kết thúc game.
/// ─── SETUP ───────────────────────────────────────────────────────────────────
/// 1. Thêm vào Scene (không cần NetworkObject).
/// 2. Gán scoreboardText (hiện khi giữ Tab), endGamePanel, endGameText.
/// 3. Điều chỉnh killLimit (mặc định 10 kills để kết thúc trận).
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Rules")]
    [Tooltip("Số kill cần đạt để thắng trận.")]
    public int killLimit = 10;

    [Header("UI")]
    [Tooltip("TMP_Text hiển thị bảng điểm (Tab). Có thể để null.")]
    public TMP_Text scoreboardText;

    [Tooltip("Panel hiện khi game kết thúc.")]
    public GameObject endGamePanel;

    [Tooltip("Text trên end game panel.")]
    public TMP_Text endGameText;

    [Header("Toast UI")]
    public GameObject toastPanel;
    public TMP_Text toastText;

    // ── Private ─────────────────────────────────────────────────────────────
    private List<PlayerController> _players = new List<PlayerController>();
    private bool _gameOver = false;

    void Awake()
    {
        Instance = this;
    }

    // ── Player Registration (gọi từ PlayerController.Spawned/Despawned) ──────

    public void RegisterPlayer(PlayerController pc)
    {
        if (!_players.Contains(pc))
            _players.Add(pc);
        Debug.Log($"[GameManager] Đăng ký P{pc.Object?.InputAuthority.PlayerId}. Tổng: {_players.Count}");
    }

    public void UnregisterPlayer(PlayerController pc)
    {
        _players.Remove(pc);
    }

    // ── Unity Update ─────────────────────────────────────────────────────────

    void Update()
    {
        if (_gameOver) return;

        CheckWinCondition();

        // Scoreboard hiện khi giữ Tab
        if (scoreboardText != null)
        {
            bool showBoard = Input.GetKey(KeyCode.Tab);
            scoreboardText.gameObject.SetActive(showBoard);
            if (showBoard) RefreshScoreboard();
        }
    }

    // ── Win Condition ────────────────────────────────────────────────────────

    private void CheckWinCondition()
    {
        _players.RemoveAll(p => p == null || p.Object == null);

        foreach (var pc in _players)
        {
            if (pc.Kills >= killLimit)
            {
                _gameOver = true;
                int pid = pc.Object.InputAuthority.PlayerId;
                ShowEndGame($"🏆  Player {pid} THẮNG!\nKills: {pc.Kills}");

                // (PlayFab đã bỏ)

                break;
            }
        }
    }

    // ── Scoreboard ───────────────────────────────────────────────────────────

    private void RefreshScoreboard()
    {
        if (scoreboardText == null) return;

        var sorted = new List<PlayerController>(_players);
        sorted.Sort((a, b) => b.Kills.CompareTo(a.Kills));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>══════ SCOREBOARD ══════</b>");
        sb.AppendLine("<color=grey>(Giữ Tab để xem)</color>\n");
        sb.AppendLine(" #  Player  │ Kills │  HP ");
        sb.AppendLine("─────────────────────────────");

        for (int i = 0; i < sorted.Count; i++)
        {
            var pc  = sorted[i];
            if (pc == null || pc.Object == null) continue;
            int pid = pc.Object.InputAuthority.PlayerId;
            string medal = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : $" {i + 1} ";
            sb.AppendLine($"{medal} P{pid,-5} │  {pc.Kills,-4} │ {pc.HP}");
        }

        scoreboardText.text = sb.ToString();
    }

    // ── End Game ─────────────────────────────────────────────────────────────

    private void ShowEndGame(string msg)
    {
        if (endGamePanel != null) endGamePanel.SetActive(true);
        if (endGameText  != null) endGameText.text = msg;

        Time.timeScale       = 0f;
        Cursor.lockState     = CursorLockMode.None;
        Cursor.visible       = true;

        Debug.Log($"[GameManager] 🏁 Game Over! {msg}");
    }

    // ── Toast Notification ───────────────────────────────────────────────────

    public void ShowPlayerLeftPopup()
    {
        if (toastPanel != null && toastText != null)
        {
            toastText.text = "A player has left the match";
            toastPanel.SetActive(true);
            Invoke(nameof(HideToast), 4f);
        }
    }

    private void HideToast()
    {
        if (toastPanel != null)
            toastPanel.SetActive(false);
    }
}
