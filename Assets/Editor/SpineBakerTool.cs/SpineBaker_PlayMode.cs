using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// SPINE BAKER - PLAY MODE VERSION
/// ✅ Giải quyết vấn đề: AnimationMode không trigger Spine runtime vertex deformation
/// 
/// Tool này bake TRONG PLAY MODE khi Spine runtime đang chạy thật sự.
/// Dành cho Spine 2D animations hoặc các animation system custom không hoạt động với AnimationMode.
/// </summary>
public class SpineBaker_PlayMode : EditorWindow
{
    [System.Serializable]
    public class AnimationToBake
    {
        public string animationName;
        public float duration;
        public bool enabled = true;
    }

    private float targetFPS = 15.0f;
    private float yOffset = 0.0f;
    private bool useManualPosition = false;
    private float vfxThreshold = 0.2f;
    private int targetSize = 512;
    
    private List<AnimationToBake> animations = new List<AnimationToBake>();
    private Vector2 scrollPos;
    
    [MenuItem("Tools/Abel/Spine Baker PLAY MODE (For Spine 2D)")]
    static void Init()
    {
        SpineBaker_PlayMode window = GetWindow<SpineBaker_PlayMode>();
        window.titleContent = new GUIContent("Spine Baker (Play Mode)");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    void OnSelectionChange() 
    { 
        RefreshAnimationList(); 
        Repaint(); 
    }

    void OnEnable() 
    { 
        RefreshAnimationList(); 
    }

    void RefreshAnimationList()
    {
        animations.Clear();
        GameObject selected = Selection.activeGameObject;
        if (selected == null) return;

        Animator animator = selected.GetComponent<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null) return;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            animations.Add(new AnimationToBake 
            { 
                animationName = clip.name, 
                duration = clip.length,
                enabled = true
            });
        }
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "⚠️ TOOL NÀY CHẠY TRONG PLAY MODE!\n\n" +
            "Dành cho Spine 2D hoặc animation systems không hoạt động với AnimationMode.\n\n" +
            "Cách dùng:\n" +
            "1. Chọn GameObject trong Scene\n" +
            "2. Cấu hình bên dưới\n" +
            "3. Nhấn PLAY trong Unity\n" +
            "4. Nhấn nút Bake\n" +
            "5. Chờ bake xong rồi Stop Play Mode",
            MessageType.Warning
        );

        GUILayout.Space(10);

        if (Selection.activeGameObject == null)
        {
            EditorGUILayout.HelpBox("⚠️ Chưa chọn GameObject!", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField($"Selected: {Selection.activeGameObject.name}", EditorStyles.boldLabel);

        GUILayout.Space(10);
        
        GUILayout.Label("CẤU HÌNH CAPTURE", EditorStyles.boldLabel);
        GUILayout.BeginVertical("box");
        targetFPS = EditorGUILayout.Slider("FPS", targetFPS, 5, 60);
        targetSize = EditorGUILayout.IntPopup("Size", targetSize, 
            new string[] { "512x512", "256x256", "128x128" },
            new int[] { 512, 256, 128 });
        
        GUILayout.Space(5);
        useManualPosition = EditorGUILayout.ToggleLeft("Giữ nguyên vị trí Unit", useManualPosition);
        if (!useManualPosition) 
            yOffset = EditorGUILayout.FloatField("Hạ thấp Unit (Y)", yOffset);
        
        vfxThreshold = EditorGUILayout.Slider("Lọc viền đen VFX", vfxThreshold, 0f, 0.5f);
        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.Label("ANIMATIONS ĐỂ BAKE", EditorStyles.boldLabel);
        if (animations.Count == 0)
        {
            EditorGUILayout.HelpBox("Không tìm thấy animation clips!", MessageType.Warning);
        }
        else
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
            foreach (var anim in animations)
            {
                EditorGUILayout.BeginHorizontal();
                anim.enabled = EditorGUILayout.Toggle(anim.enabled, GUILayout.Width(20));
                EditorGUILayout.LabelField(anim.animationName, GUILayout.Width(150));
                EditorGUILayout.LabelField($"{anim.duration:F2}s", GUILayout.Width(50));
                EditorGUILayout.LabelField($"{Mathf.CeilToInt(anim.duration * targetFPS)} frames", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(20);

        if (!EditorApplication.isPlaying)
        {
            GUI.backgroundColor = Color.yellow;
            EditorGUILayout.HelpBox("⚠️ Phải PLAY Unity trước khi bake!", MessageType.Warning);
            GUI.backgroundColor = Color.white;
        }
        else
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🎬 BẮT ĐẦU BAKE TRONG PLAY MODE", GUILayout.Height(50)))
            {
                StartBaking();
            }
            GUI.backgroundColor = Color.white;
        }
    }

    void StartBaking()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogError("Không có GameObject được chọn!");
            return;
        }

        Animator animator = selected.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("GameObject thiếu Animator!");
            return;
        }

        // Start coroutine trong Play Mode
        var helper = selected.GetComponent<BakingHelper>();
        if (helper == null)
            helper = selected.AddComponent<BakingHelper>();

        helper.StartBaking(selected, animator, animations, targetFPS, targetSize, yOffset, useManualPosition, vfxThreshold);
    }
}

/// <summary>
/// Helper component chạy trong Play Mode để bake animation
/// </summary>
public class BakingHelper : MonoBehaviour
{
    public void StartBaking(
        GameObject target, 
        Animator animator,
        List<SpineBaker_PlayMode.AnimationToBake> animations,
        float fps,
        int size,
        float yOffset,
        bool useManualPos,
        float vfxThreshold)
    {
        StartCoroutine(BakeCoroutine(target, animator, animations, fps, size, yOffset, useManualPos, vfxThreshold));
    }

    IEnumerator BakeCoroutine(
        GameObject target,
        Animator animator,
        List<SpineBaker_PlayMode.AnimationToBake> animations,
        float fps,
        int size,
        float yOffset,
        bool useManualPos,
        float vfxThreshold)
    {
        Camera bakingCam = GameObject.Find("BakingCam")?.GetComponent<Camera>();
        if (bakingCam == null)
        {
            Debug.LogError("Không tìm thấy BakingCam!");
            yield break;
        }

        // Setup Camera
        if (bakingCam.targetTexture != null) bakingCam.targetTexture.Release();
        RenderTexture bakeRT = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        bakingCam.targetTexture = bakeRT;
        bakingCam.clearFlags = CameraClearFlags.SolidColor;
        bakingCam.backgroundColor = new Color(0, 0, 0, 0);

        Vector3 originalPos = target.transform.position;
        Quaternion originalRot = target.transform.rotation;

        if (!useManualPos)
        {
            target.transform.position = new Vector3(0, -yOffset, 0);
            target.transform.rotation = Quaternion.identity;
        }

        string folderPath = $"Assets/BakedSpine_PlayMode/{target.name}";
        if (Directory.Exists(folderPath)) Directory.Delete(folderPath, true);
        Directory.CreateDirectory(folderPath);

        Debug.Log($"<color=cyan>🎬 Bắt đầu bake {animations.Count} animations...</color>");

        foreach (var animInfo in animations)
        {
            if (!animInfo.enabled) continue;

            Debug.Log($"<color=yellow>📹 Baking: {animInfo.animationName}</color>");

            // Play animation
            animator.Play(animInfo.animationName, 0, 0f);
            yield return null; // Wait 1 frame for animation to start

            int frameCount = Mathf.CeilToInt(animInfo.duration * fps);
            float frameTime = 1f / fps;

            for (int i = 0; i < frameCount; i++)
            {
                // Wait for animation to reach this time
                float normalizedTime = (float)i / frameCount;
                animator.Play(animInfo.animationName, 0, normalizedTime);
                
                // CRITICAL: Wait 2 frames để Spine runtime cập nhật mesh vertices
                yield return null;
                yield return null;

                // Render
                bakingCam.Render();

                // Capture
                Texture2D tempTex = new Texture2D(size, size, TextureFormat.ARGB32, false);
                RenderTexture.active = bakeRT;
                tempTex.ReadPixels(new Rect(0, 0, size, size), 0, 0);

                // Fix VFX alpha
                Color[] pixels = tempTex.GetPixels();
                for (int p = 0; p < pixels.Length; p++)
                {
                    Color c = pixels[p];
                    float maxRGB = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                    if (maxRGB > vfxThreshold && c.a < maxRGB)
                    {
                        c.a = maxRGB;
                        pixels[p] = c;
                    }
                }
                tempTex.SetPixels(pixels);
                tempTex.Apply();

                // Save
                byte[] bytes = tempTex.EncodeToPNG();
                File.WriteAllBytes($"{folderPath}/{animInfo.animationName}_{i:D3}.png", bytes);
                DestroyImmediate(tempTex);
            }

            Debug.Log($"<color=green>✅ {animInfo.animationName}: {frameCount} frames</color>");
        }

        // Cleanup
        bakingCam.targetTexture = null;
        bakeRT.Release();

        if (!useManualPos)
        {
            target.transform.position = originalPos;
            target.transform.rotation = originalRot;
        }

        AssetDatabase.Refresh();
        Debug.Log($"<color=lime>🎉 HOÀN TẤT! Kiểm tra folder: {folderPath}</color>");
        
#if UNITY_EDITOR
        EditorUtility.RevealInFinder(folderPath);
#endif

        // Destroy helper
        Destroy(this);
    }
}
