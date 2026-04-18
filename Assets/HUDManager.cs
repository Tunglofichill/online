using UnityEngine;
using TMPro;

/// <summary>
/// HUDManager – hiển thị HP, số Kill và đồng hồ đếm ngược đồng bộ.
/// ─── SETUP ───────────────────────────────────────────────────────────────────
/// 1. Tạo Canvas (World Space hoặc Screen Space Overlay) trong Scene.
/// 2. Thêm các TMP_Text cho: hpText, killsText, timerText, crosshairText.
/// 3. Gán HUDManager vào Canvas hoặc bất kỳ GameObject nào trong Scene.
/// 4. Canvas bị ẩn mặc định – sẽ tự hiện khi player spawn xong.
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text hpText;
    public TMP_Text killsText;
    public TMP_Text timerText;
    public TMP_Text crosshairText;   // Dùng ký tự "+" làm crosshair

    [Header("Settings")]
    [Tooltip("Thời gian mỗi trận (giây). Mặc định 300s = 5 phút.")]
    public float gameDuration = 300f;

    private float _timeLeft;
    private bool  _isRunning = false;

    void Start()
    {
        _timeLeft = gameDuration;

        // Crosshair mặc định
        if (crosshairText != null)
            crosshairText.text = "<size=24><b>+</b></size>";

        // HUD ẩn cho đến khi player local spawn xong
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_isRunning || _timeLeft <= 0f) return;
        _timeLeft -= Time.deltaTime;
        RefreshTimer();
    }

    // ── Public API ──────────────────────────────────────────────────────────

    public void StartTimer() => _isRunning = true;
    public void StopTimer()  => _isRunning = false;

    public void UpdateHP(int hp)
    {
        if (hpText == null) return;
        string color = hp > 60 ? "green" : hp > 30 ? "yellow" : "red";
        hpText.text = $"<color={color}>❤  {hp} / 100</color>";
    }

    public void UpdateKills(int kills)
    {
        if (killsText != null)
            killsText.text = $"🎯  Kills: <b>{kills}</b>";
    }

    // ── Private ─────────────────────────────────────────────────────────────

    private void RefreshTimer()
    {
        if (timerText == null) return;
        int m = Mathf.FloorToInt(_timeLeft / 60f);
        int s = Mathf.FloorToInt(_timeLeft % 60f);
        timerText.text = $"⏱  {m:00}:{s:00}";
    }
}
