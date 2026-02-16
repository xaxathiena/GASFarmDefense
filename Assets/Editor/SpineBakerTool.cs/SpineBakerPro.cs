using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Spine Baker PRO (Full Options)
/// - Fix lỗi Mesh, Viền đen.
/// - Thêm tùy chọn xuất ảnh PNG từng frame để kiểm tra.
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
    private CompressionType compression = CompressionType.Uncompressed_RGBA32; 

    // --- TÙY CHỌN DEBUG MỚI ---
    private bool exportIndividualPNGs = false; // <--- Checkbox mới

    // --- TRẠNG THÁI UI ---
    private bool isValidSelection = false;
    private string statusMessage = "";
    private MessageType statusType = MessageType.None;

    [MenuItem("Tools/Abel/Spine Baker PRO (Full Options)")]
    static void Init()
    {
        SpineBakerPro window = GetWindow<SpineBakerPro>();
        window.titleContent = new GUIContent("Baker Pro");
        window.minSize = new Vector2(350, 650);
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
        
        GUILayout.Space(5);
        // --- CHECKBOX MỚI ---
        exportIndividualPNGs = EditorGUILayout.ToggleLeft("Xuất thêm ảnh PNG từng frame (Debug)", exportIndividualPNGs);
        if (exportIndividualPNGs) 
        {
            EditorGUILayout.HelpBox("Sẽ tạo folder 'Assets/BakedFrames/...' chứa ảnh.", MessageType.Warning);
        }
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
        bakingCam.backgroundColor = new Color(0,0,0,0); 

        bool wasAnimatorEnabled = animator.enabled;
        animator.enabled = false;
        Vector3 originalPos = selected.transform.position;
        Quaternion originalRot = selected.transform.rotation;
        if (!useManualPosition) {
            selected.transform.position = new Vector3(0, -yOffset, 0);
            selected.transform.rotation = Quaternion.identity;
        }

        // --- TẠO FOLDER PNG NẾU CẦN ---
        string pngFolderPath = $"Assets/BakedFrames/{selected.name}";
        if (exportIndividualPNGs)
        {
            if (Directory.Exists(pngFolderPath)) Directory.Delete(pngFolderPath, true);
            Directory.CreateDirectory(pngFolderPath);
        }
        // -------------------------------

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
                info.speedModifier = 1.0f; // Mặc định speed chuẩn
                animDataList.Add(info);

                if (EditorUtility.DisplayCancelableProgressBar("Baking...", $"Anim: {clip.name}", (float)i / clips.Length)) break;

                for (int f = 0; f < framesInClip; f++)
                {
                    float time = f / targetFPS;
                    if (time > clip.length) time = clip.length;

                    // 1. Update Animation
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
                    
                    // 3. Process Pixels (Khử viền đen)
                    Color[] pixels = tempTex.GetPixels();
                    for (int p = 0; p < pixels.Length; p++)
                    {
                        Color c = pixels[p];
                        if (c.a > 0.0f) { c.r /= c.a; c.g /= c.a; c.b /= c.a; } // Un-multiply Alpha

                        float maxRGB = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                        if (maxRGB > vfxThreshold && c.a < maxRGB) { c.a = maxRGB; } // Fix VFX Glow
                        
                        c.r = Mathf.Clamp01(c.r); c.g = Mathf.Clamp01(c.g); c.b = Mathf.Clamp01(c.b); c.a = Mathf.Clamp01(c.a);
                        pixels[p] = c;
                    }
                    tempTex.SetPixels(pixels); 
                    tempTex.Apply();

                    // --- XUẤT PNG NẾU CẦN ---
                    if (exportIndividualPNGs)
                    {
                        byte[] bytes = tempTex.EncodeToPNG();
                        File.WriteAllBytes($"{pngFolderPath}/{clip.name}_{f:D3}.png", bytes);
                    }
                    // ------------------------

                    // 4. Compress & Copy to Array
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
            
            string msg = "HOÀN TẤT!";
            if (exportIndividualPNGs) msg += $"\nĐã xuất ảnh frame vào: {pngFolderPath}";
            Debug.Log(msg);
            
            if (exportIndividualPNGs) EditorUtility.RevealInFinder(pngFolderPath);
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