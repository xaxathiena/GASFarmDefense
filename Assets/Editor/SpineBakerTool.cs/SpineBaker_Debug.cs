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
    
    private int targetSize = 512; 

    [MenuItem("Tools/Abel/Spine Baker (DEBUG FIX MESH)")]
    static void Init()
    {
        SpineBaker_Debug window = GetWindow<SpineBaker_Debug>();
        window.titleContent = new GUIContent("Baker Debug Fixed");
        window.minSize = new Vector2(350, 450);
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("DEBUG MODE: SỬA LỖI ĐỨNG HÌNH", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Sử dụng AnimationMode để ép SkinnedMesh cập nhật theo xương.", MessageType.Info);

        GUILayout.Space(10);
        targetFPS = EditorGUILayout.Slider("FPS", targetFPS, 1, 60);
        
        GUILayout.Space(5);
        useManualPosition = EditorGUILayout.ToggleLeft("Giữ nguyên vị trí Unit", useManualPosition);
        if (!useManualPosition) yOffset = EditorGUILayout.FloatField("Hạ thấp Unit (Y)", yOffset);
        
        vfxThreshold = EditorGUILayout.Slider("Lọc viền đen VFX", vfxThreshold, 0f, 0.5f);

        GUILayout.Space(20);

        if (GUILayout.Button("CHỤP PNG (FIXED MESH)", GUILayout.Height(40)))
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
        bakingCam.backgroundColor = new Color(0,0,0,0);

        // Chuẩn bị Animation
        bool wasAnimatorEnabled = animator.enabled;
        animator.enabled = false; 
        
        Vector3 originalPos = selected.transform.position;
        Quaternion originalRot = selected.transform.rotation;

        if (!useManualPosition) {
            selected.transform.position = new Vector3(0, -yOffset, 0);
            selected.transform.rotation = Quaternion.identity;
        }

        string folderPath = $"Assets/BakedSpine_Debug/{selected.name}";
        if (Directory.Exists(folderPath)) Directory.Delete(folderPath, true);
        Directory.CreateDirectory(folderPath);

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        // --- BẮT ĐẦU CHẾ ĐỘ ANIMATION MODE (QUAN TRỌNG) ---
        if (!AnimationMode.InAnimationMode())
            AnimationMode.StartAnimationMode();

        try 
        {
            foreach (var clip in clips)
            {
                int frameCount = Mathf.FloorToInt(clip.length * targetFPS);
                if (frameCount < 1) frameCount = 1;

                for (int i = 0; i < frameCount; i++)
                {
                    float time = i / targetFPS;
                    if (time > clip.length) time = clip.length;

                    // 1. Kích hoạt Animation Mode
                    AnimationMode.BeginSampling();
                    
                    // 2. Sample bằng AnimationMode (Thay cho clip.SampleAnimation)
                    // Hàm này ép Unity cập nhật Scene View và SkinnedMesh ngay lập tức
                    AnimationMode.SampleAnimationClip(selected, clip, time);
                    
                    AnimationMode.EndSampling();

                    // 3. CRITICAL: Force update ALL child transforms & renderers
                    // Với skeleton có nhiều children bones, cần multiple repaint
                    SceneView.RepaintAll();
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    
                    // Force update tất cả SkinnedMesh và MeshRenderer trong children
                    SkinnedMeshRenderer[] skinRenderers = selected.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var skr in skinRenderers)
                    {
                        if (skr.enabled && skr.gameObject.activeInHierarchy)
                        {
                            skr.forceMatrixRecalculationPerRender = true;
                        }
                    }

                    // 4. Force Update VFX (Particle System nếu có)
                    ParticleSystem[] particles = selected.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in particles)
                    {
                        ps.Simulate(time, true, true);
                    }

                    // 5. Final repaint để đảm bảo tất cả đã update
                    SceneView.RepaintAll();
                    
                    // 6. Render
                    bakingCam.Render();

                    // 5. Lưu ảnh
                    Texture2D tempTex = new Texture2D(targetSize, targetSize, TextureFormat.ARGB32, false);
                    RenderTexture.active = bakeRT;
                    tempTex.ReadPixels(new Rect(0, 0, targetSize, targetSize), 0, 0);
                    
                    Color[] pixels = tempTex.GetPixels();
                    for (int p = 0; p < pixels.Length; p++)
                    {
                        Color c = pixels[p];
                        float maxRGB = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                        if (maxRGB > vfxThreshold && c.a < maxRGB) { c.a = maxRGB; pixels[p] = c; }
                    }
                    tempTex.SetPixels(pixels);
                    tempTex.Apply();

                    byte[] bytes = tempTex.EncodeToPNG();
                    File.WriteAllBytes($"{folderPath}/{clip.name}_{i:D3}.png", bytes);
                    DestroyImmediate(tempTex);
                }
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>Đã xuất xong! Kiểm tra folder: {folderPath}</color>");
            EditorUtility.RevealInFinder(folderPath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Lỗi: " + ex.Message);
        }
        finally
        {
            // --- KẾT THÚC ANIMATION MODE ---
            AnimationMode.StopAnimationMode();
            
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