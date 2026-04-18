using UnityEngine;
using TMPro;
using Fusion;
using System.Collections.Generic;

public class ChatManager : SimulationBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputField;
    public TMP_Text chatText;

    private List<string> messages = new List<string>();
    private bool isReady = false;

    private void Start()
    {
        Debug.Log("ChatManager Start - Đang tìm NetworkRunner...");
    }

    private void Update()
    {
        // Tự động đăng ký nếu chưa có Runner
        if (Runner == null)
        {
            var runner = NetworkRunner.GetRunnerForGameObject(gameObject);
            if (runner != null)
            {
                runner.AddGlobal(this);
                Debug.Log("✅ ChatManager: Đã đăng ký AddGlobal với NetworkRunner");
            }
        }

        // Kiểm tra LocalPlayer đã sẵn sàng chưa
        if (!isReady && Runner != null && Runner.LocalPlayer != null)
        {
            isReady = true;
            Debug.Log("🎉 ChatManager: ĐÃ SẴN SÀNG HOÀN TOÀN! LocalPlayer = P" + Runner.LocalPlayer.PlayerId);
        }

        // Nhấn Enter để gửi
        if (isReady && inputField != null && inputField.isFocused && Input.GetKeyDown(KeyCode.Return))
        {
            SendMessage();
        }
    }

    public void SendMessage()
    {
        if (!isReady || Runner == null || Runner.LocalPlayer == null)
        {
            Debug.LogWarning("ChatManager: Chưa sẵn sàng! Vui lòng chờ thêm 1-2 giây sau khi player spawn.");
            return;
        }

        if (string.IsNullOrWhiteSpace(inputField.text)) return;

        string playerName = "P" + Runner.LocalPlayer.PlayerId;
        string fullMsg = playerName + ": " + inputField.text.Trim();

        RPC_SendChatMessage(Runner, fullMsg);

        inputField.text = "";
        inputField.ActivateInputField();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public static void RPC_SendChatMessage(NetworkRunner runner, string msg)
    {
        var chatManager = FindObjectOfType<ChatManager>(true);
        if (chatManager != null)
        {
            chatManager.AddMessage(msg);
        }
    }

    private void AddMessage(string msg)
    {
        messages.Add(msg);
        if (messages.Count > 50) messages.RemoveAt(0);

        if (chatText != null)
            chatText.text = string.Join("\n", messages);
    }
}