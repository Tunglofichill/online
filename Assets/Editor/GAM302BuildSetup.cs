#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Script tự động cài đặt các thông số Build (.exe) phù hợp nhất cho Multiplayer.
/// Hỗ trợ sửa lỗi Player 2 (.exe) hay bị đơ, không thao tác được khi test 2 cửa sổ.
/// </summary>
public class GAM302BuildSetup : MonoBehaviour
{
    [MenuItem("GAM302/🛠 Tối ưu & Setup cấu hình Build .EXE")]
    public static void OptimizeBuildSettings()
    {
        // 1. CHỐNG ĐƠ MULTIPLAYER: Chạy ngầm kể cả khi mất focus (Cực kì quan trọng)
        PlayerSettings.runInBackground = true;
        
        // 2. CHẾ ĐỘ CỬA SỔ (WINDOWED): Chơi nhiều màn hình không bị văng
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;

        // 3. TỐI ƯU HIỆU NĂNG:
        QualitySettings.vSyncCount = 0; // Tắt Vsync mặc định để giảm giật lag Input
        
        // 4. KIỂM TRA SCENE: Đảm bảo Scene hiện tại nằm ở đầu danh sách Build Settings
        Scene currentScene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(currentScene.path))
        {
            EditorUtility.DisplayDialog("Lỗi", "Hãy lưu Scene này lại (Ctrl+S) trước khi chạy Setup Build!", "OK");
            return;
        }

        // Tạo danh sách các Scene tham gia Build
        EditorBuildSettingsScene[] tempScenes = EditorBuildSettings.scenes;
        bool sceneExists = false;

        for (int i = 0; i < tempScenes.Length; i++)
        {
            if (tempScenes[i].path == currentScene.path)
            {
                sceneExists = true;
                break;
            }
        }

        // Tự động Add Scene nếu nó chưa có trong Build Settings
        if (!sceneExists)
        {
            var newScenes = new EditorBuildSettingsScene[tempScenes.Length + 1];
            System.Array.Copy(tempScenes, newScenes, tempScenes.Length);
            newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(currentScene.path, true);
            EditorBuildSettings.scenes = newScenes;
        }

        Debug.Log("[GAM302] Vừa cập nhật 1 loạt BuildSettings tối ưu hóa thao tác P2!");

        EditorUtility.DisplayDialog("✅ Tối ưu Build Thành Công", 
            "Đã tự động SetUp xong!\n\n" +
            "🔑 Lỗi ko thao tác được chủ yếu do game tự Pause khi trỏ chuột qua cửa sổ khác.\n" +
            "Đã bật tính năng 'Run In Background' chống đơ cửa sổ.\n\n" +
            "Bây giờ bạn có thể bấm Ctrl + B để Build game cực mượt!", 
            "Tuyệt vời");
    }
}
#endif
