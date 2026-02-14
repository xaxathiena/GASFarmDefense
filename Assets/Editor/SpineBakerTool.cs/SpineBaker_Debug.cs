using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class SpineBaker_Debug : EditorWindow
{
    // --- CẤU HÌNH ---
    private float targetFPS = 15.0f;
    private float yOffset = 0.0f;
    private bool useManualPosition = false;
    private float vfxThreshold = 0.2f;
    
    // Size cố định để test cho nhanh
    private int targetSize = 512; 

    [MenuItem("Tools/Abel/Spine Baker (DEBUG PNG)")]
    static void Init()
    {
        SpineBaker_Debug window = GetWindow<SpineBaker_Debug>();
        window.titleContent = new GUIContent("Baker DEBUG");
        window.minSize = new Vector2(350, 400);
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("CHẾ ĐỘ DEBUG: XUẤT ẢNH PNG", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Tool này sẽ xuất từng frame ra file ảnh để kiểm tra chuyển động.", MessageType.Info);

        GUILayout.Space(10);
        targetFPS = EditorGUILayout.Slider("FPS", targetFPS, 1, 60);
        
        GUILayout.Space(5);
        useManualPosition = EditorGUILayout.ToggleLeft("Giữ nguyên vị trí Unit", useManualPosition);
        if (!useManualPosition) yOffset = EditorGUILayout.FloatField("Hạ thấp Unit (Y)", yOffset);
        
        vfxThreshold = EditorGUILayout.Slider("Lọc viền đen VFX", vfxThreshold, 0f, 0.5f);

        GUILayout.Space(20);

        if (GUILayout.Button("CHỤP TỪNG TẤM PNG NGAY", GUILayout.Height(40)))
        {
            CaptureFrames();
        }
    }

    void CaptureFrames()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null) { Debug.LogError("Chưa chọn Unit!"); return; }

        Animator animator = selected.GetComponent<Animator>();
        Camera bakingCam = GameObject.Find("BakingCam")?.GetComponent<Camera>();

        // Setup Camera
        if (bakingCam.targetTexture != null) bakingCam.targetTexture.Release();
        RenderTexture bakeRT = new RenderTexture(targetSize, targetSize, 24, RenderTextureFormat.ARGB32);
        bakingCam.targetTexture = bakeRT;
        bakingCam.clearFlags = CameraClearFlags.SolidColor;
        bakingCam.backgroundColor = new Color(0,0,0,0); // Trong suốt

        // Setup Animation
        bool wasAnimatorEnabled = animator.enabled;
        animator.enabled = false; // Tắt Animator để điều khiển thủ công
        
        Vector3 originalPos = selected.transform.position;
        Quaternion originalRot = selected.transform.rotation;

        if (!useManualPosition) {
            selected.transform.position = new Vector3(0, -yOffset, 0);
            selected.transform.rotation = Quaternion.identity;
        }

        // Tạo folder lưu ảnh
        string folderPath = $"Assets/BakedSpine_Debug/{selected.name}";
        if (Directory.Exists(folderPath)) Directory.Delete(folderPath, true); // Xóa cũ
        Directory.CreateDirectory(folderPath);

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        try 
        {
            // Duyệt từng Clip
            foreach (var clip in clips)
            {
                int frameCount = Mathf.FloorToInt(clip.length * targetFPS);
                if (frameCount < 1) frameCount = 1;

                for (int i = 0; i < frameCount; i++)
                {
                    float time = i / targetFPS;
                    if (time > clip.length) time = clip.length;

                    // --- BƯỚC QUAN TRỌNG NHẤT ---
                    // 1. Đặt animation vào thời điểm 'time'
                    clip.SampleAnimation(selected, time); 
                    
                    // 2. Ép hệ thống vật lý/transform cập nhật (đề phòng bone bị lag)
                    Physics.SyncTransforms(); 

                    // 3. Render Camera (Tuyệt đối KHÔNG dùng BakeMesh)
                    bakingCam.Render();

                    // 4. Lưu ảnh
                    Texture2D tempTex = new Texture2D(targetSize, targetSize, TextureFormat.ARGB32, false);
                    RenderTexture.active = bakeRT;
                    tempTex.ReadPixels(new Rect(0, 0, targetSize, targetSize), 0, 0);
                    
                    // Fix VFX (Lọc viền đen)
                    Color[] pixels = tempTex.GetPixels();
                    for (int p = 0; p < pixels.Length; p++)
                    {
                        Color c = pixels[p];
                        float maxRGB = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                        if (maxRGB > vfxThreshold && c.a < maxRGB) { c.a = maxRGB; pixels[p] = c; }
                    }
                    tempTex.SetPixels(pixels);
                    tempTex.Apply();

                    // Xuất ra file
                    byte[] bytes = tempTex.EncodeToPNG();
                    File.WriteAllBytes($"{folderPath}/{clip.name}_{i:D3}.png", bytes);
                    DestroyImmediate(tempTex);
                }
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>Đã xuất xong ảnh vào: {folderPath}</color>");
            EditorUtility.RevealInFinder(folderPath); // Tự mở thư mục lên cho anh xem
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Lỗi: " + ex.Message);
        }
        finally
        {
            bakingCam.targetTexture = null;
            bakeRT.Release();
            animator.enabled = wasAnimatorEnabled;
            if (!useManualPosition) {
                selected.transform.position = originalPos;
                selected.transform.rotation = originalRot;
            }
        }
    }
}