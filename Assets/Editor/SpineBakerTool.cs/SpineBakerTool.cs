using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Spine Baker PRO (Final Clean)
/// - Fix lỗi Mesh cứng đờ.
/// - Fix lỗi viền đen (Anti-Black Border).
/// </summary>
public class SpineBakerPro : EditorWindow
{
    // --- CẤU HÌNH CAPTURE ---
    private float targetFPS = 15.0f;
    private float yOffset = 0.0f;
    private bool useManualPosition = false;
    private float vfxThreshold = 0.2f;

    // --- CẤU HÌNH OUTPUT ---
    public enum OutputSize { _512 = 512, _256 = 256, _128 = 128 }
    private OutputSize targetSize = OutputSize._256; 

    public enum CompressionType 
    { 
        PC_High_BC7 = TextureFormat.BC7, 
        PC_Normal_DXT5 = TextureFormat.DXT5, 
        Mobile_High_ASTC = TextureFormat.ASTC_6x6, 
        Mobile_Fast_ETC2 = TextureFormat.ETC2_RGBA8,
        Uncompressed_RGBA32 = TextureFormat.RGBA32
    }
    // Vẫn nên để Uncompressed lúc test cho đẹp
    private CompressionType compression = CompressionType.Uncompressed_RGBA32; 

    // --- TRẠNG THÁI UI ---
    private bool isValidSelection = false;
    private string statusMessage = "";
    private MessageType statusType = MessageType.None;

    [MenuItem("Tools/Abel/Spine Baker PRO (Anti-Black Border)")]
    static void Init()
    {
        SpineBakerPro window = GetWindow<SpineBakerPro>();
        window.titleContent = new GUIContent("Baker Clean");
        window.minSize = new Vector2(350, 600);
        window.Show();
    }

    private void OnSelectionChange() { ValidateSelection(); Repaint(); }
    private void OnEnable() { ValidateSelection(); }

    void ValidateSelection()
    {
        GameObject obj = Selection.activeGameObject;
        isValidSelection = false;
        
        if (obj == null) {
            statusMessage = "Chưa chọn Object nào."; statusType = MessageType.Info; return;
        }
        Animator anim = obj.GetComponent<Animator>();
        if (anim == null) {
            statusMessage = "Lỗi: Object thiếu Animator!"; statusType = MessageType.Error; return;
        }
        if (anim.runtimeAnimatorController == null || anim.runtimeAnimatorController.animationClips.Length == 0) {
            statusMessage = "Lỗi: Animator không có Clips!"; statusType = MessageType.Warning; return;
        }
        Camera cam = GameObject.Find("BakingCam")?.GetComponent<Camera>();
        if (cam == null) {
            statusMessage = "Lỗi Critical: Thiếu 'BakingCam'!"; statusType = MessageType.Error; return;
        }

        isValidSelection = true;
        statusMessage = $"SẴN SÀNG: {obj.name} ({anim.runtimeAnimatorController.animationClips.Length} clips)";
        statusType = MessageType.Info;
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        if (isValidSelection) {
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            GUI.backgroundColor = Color.white;
        } else {
            EditorGUILayout.HelpBox(statusMessage, statusType);
        }

        GUILayout.Space(10);
        
        GUILayout.Label("1. CẤU HÌNH ANIMATION", EditorStyles.boldLabel);
        GUILayout.BeginVertical("box");
        targetFPS = EditorGUILayout.Slider("FPS (Độ mượt)", targetFPS, 1, 60);
        EditorGUILayout.LabelField("Dung lượng:", $"{(targetFPS/30f)*100:F0}% so với gốc", EditorStyles.miniLabel);
        GUILayout.Space(5);
        useManualPosition = EditorGUILayout.ToggleLeft("Giữ nguyên vị trí Unit", useManualPosition);
        if (!useManualPosition) yOffset = EditorGUILayout.FloatField("Hạ thấp Unit (Y)", yOffset);
        vfxThreshold = EditorGUILayout.Slider("Lọc viền đen VFX", vfxThreshold, 0f, 0.5f);
        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.Label("2. CẤU HÌNH OUTPUT", EditorStyles.boldLabel);
        GUILayout.BeginVertical("box");
        targetSize = (OutputSize)EditorGUILayout.EnumPopup("Kích thước Texture:", targetSize);
        compression = (CompressionType)EditorGUILayout.EnumPopup("Chuẩn nén:", compression);
        EditorGUILayout.HelpBox("Khuyên dùng Uncompressed_RGBA32 để debug hình ảnh trước.", MessageType.Info);
        GUILayout.EndVertical();

        GUILayout.Space(20);

        EditorGUI.BeginDisabledGroup(!isValidSelection);
        if (GUILayout.Button("BAKE & CREATE DATA NGAY", GUILayout.Height(50)))
        {
            BakeAndPackage();
        }
        EditorGUI.EndDisabledGroup();
    }

    void BakeAndPackage()
    {
        GameObject selected = Selection.activeGameObject;
        Animator animator = selected.GetComponent<Animator>();
        Camera bakingCam = GameObject.Find("BakingCam").GetComponent<Camera>();

        int size = (int)targetSize;
        
        if (bakingCam.targetTexture != null) bakingCam.targetTexture.Release();
        RenderTexture bakeRT = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        bakingCam.targetTexture = bakeRT;
        bakingCam.clearFlags = CameraClearFlags.SolidColor;
        bakingCam.backgroundColor = new Color(0,0,0,0); // Nền đen trong suốt

        bool wasAnimatorEnabled = animator.enabled;
        animator.enabled = false;
        Vector3 originalPos = selected.transform.position;
        Quaternion originalRot = selected.transform.rotation;
        if (!useManualPosition) {
            selected.transform.position = new Vector3(0, -yOffset, 0);
            selected.transform.rotation = Quaternion.identity;
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        int totalFrames = 0;
        foreach(var c in clips) totalFrames += Mathf.Max(1, Mathf.FloorToInt(c.length * targetFPS) + 1);

        Debug.Log($"Bắt đầu Bake: {totalFrames} frames. Size: {size}. Format: {compression}");

        Texture2DArray textureArray = new Texture2DArray(size, size, totalFrames, (TextureFormat)compression, false);
        textureArray.filterMode = FilterMode.Bilinear;
        textureArray.wrapMode = TextureWrapMode.Clamp;

        List<UnitAnimData.AnimInfo> animDataList = new List<UnitAnimData.AnimInfo>();
        int currentSlice = 0;

        if (!AnimationMode.InAnimationMode())
            AnimationMode.StartAnimationMode();

        try 
        {
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                int framesInClip = Mathf.Max(1, Mathf.FloorToInt(clip.length * targetFPS) + 1);

                UnitAnimData.AnimInfo info = new UnitAnimData.AnimInfo();
                info.animName = clip.name;
                info.startFrame = currentSlice;
                info.frameCount = framesInClip;
                info.fps = targetFPS;
                info.duration = clip.length;
                info.loop = clip.isLooping;
                info.speedModifier = 1.0f;
                animDataList.Add(info);

                if (EditorUtility.DisplayCancelableProgressBar("Baking...", $"Anim: {clip.name}", (float)i / clips.Length)) break;

                for (int f = 0; f < framesInClip; f++)
                {
                    float time = f / targetFPS;
                    if (time > clip.length) time = clip.length;

                    // 1. Update Animation & Physics
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(selected, clip, time);
                    AnimationMode.EndSampling();

                    SceneView.RepaintAll();
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    
                    SkinnedMeshRenderer[] skinRenderers = selected.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var skr in skinRenderers)
                    {
                        if (skr.enabled && skr.gameObject.activeInHierarchy)
                            skr.forceMatrixRecalculationPerRender = true;
                    }

                    ParticleSystem[] particles = selected.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in particles) ps.Simulate(time, true, true);

                    SceneView.RepaintAll();
                    bakingCam.Render();

                    // 2. Read Pixels
                    Texture2D tempTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    RenderTexture.active = bakeRT;
                    tempTex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                    
                    // --- 3. XỬ LÝ PIXEL: KHỬ VIỀN ĐEN & FIX VFX ---
                    Color[] pixels = tempTex.GetPixels();
                    for (int p = 0; p < pixels.Length; p++)
                    {
                        Color c = pixels[p];

                        // A. KHỬ VIỀN ĐEN (Un-multiply Alpha)
                        // Nếu pixel có độ trong suốt, ta chia RGB cho Alpha để lấy lại độ sáng gốc.
                        // Điều này loại bỏ việc viền bị tối đi do hòa trộn với nền đen.
                        if (c.a > 0.0f)
                        {
                            c.r /= c.a;
                            c.g /= c.a;
                            c.b /= c.a;
                        }

                        // B. FIX VFX (Giữ nguyên logic cũ để cứu hiệu ứng Glow)
                        // Vì bước A đã làm sáng pixel lên, nên logic này vẫn chạy tốt.
                        float maxRGB = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                        if (maxRGB > vfxThreshold && c.a < maxRGB) {
                            c.a = maxRGB; 
                        }
                        
                        // C. Kẹp giá trị lại (đề phòng chia ra số > 1)
                        c.r = Mathf.Clamp01(c.r);
                        c.g = Mathf.Clamp01(c.g);
                        c.b = Mathf.Clamp01(c.b);
                        c.a = Mathf.Clamp01(c.a);

                        pixels[p] = c;
                    }
                    tempTex.SetPixels(pixels); 
                    tempTex.Apply();

                    // 4. Compress & Copy
                    if (compression != CompressionType.Uncompressed_RGBA32)
                    {
                        EditorUtility.CompressTexture(tempTex, (TextureFormat)compression, TextureCompressionQuality.Best);
                    }
                    
                    Graphics.CopyTexture(tempTex, 0, 0, textureArray, currentSlice, 0);
                    DestroyImmediate(tempTex);

                    currentSlice++;
                }
            }

            // 5. Finalize
            textureArray.Apply(false, true); 
            string folderPath = "Assets/BakedData";
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            
            string texPath = $"{folderPath}/{selected.name}_Array.asset";
            AssetDatabase.CreateAsset(textureArray, texPath);

            string dataPath = $"{folderPath}/{selected.name}_Data.asset";
            UnitAnimData dataSO = ScriptableObject.CreateInstance<UnitAnimData>();
            dataSO.textureArray = textureArray;
            dataSO.animations = animDataList;
            
            AssetDatabase.CreateAsset(dataSO, dataPath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = dataSO;
            EditorGUIUtility.PingObject(dataSO);

            Debug.Log($"HOÀN TẤT! Đã fix lỗi viền đen.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Lỗi: {ex.Message}");
        }
        finally
        {
            AnimationMode.StopAnimationMode();
            EditorUtility.ClearProgressBar();
            bakingCam.targetTexture = null;
            bakeRT.Release();
            
            if (!useManualPosition) {
                selected.transform.position = originalPos;
                selected.transform.rotation = originalRot;
            }
            animator.enabled = wasAnimatorEnabled;
            AssetDatabase.Refresh();
        }
    }
}