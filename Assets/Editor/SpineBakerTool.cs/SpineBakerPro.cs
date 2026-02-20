using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Spine Baker PRO (Full Options)
/// - Fixes Mesh and Black Outline issues.
/// - Adds option to export individual PNG frames for debugging.
/// </summary>
public class SpineBakerPro : EditorWindow
{
    // --- CAPTURE CONFIGURATION ---
    private float targetFPS = 15.0f;
    private float yOffset = 0.0f;
    private bool useManualPosition = false;
    private float vfxThreshold = 0.2f;

    // --- OUTPUT CONFIGURATION ---
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

    // --- NEW DEBUG OPTIONS ---
    private bool exportIndividualPNGs = false;

    // --- UI STATE ---
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
            statusMessage = "No object selected."; statusType = MessageType.Info; return;
        }
        Animator anim = obj.GetComponent<Animator>();
        if (anim == null) {
            statusMessage = "Error: Object is missing Animator!"; statusType = MessageType.Error; return;
        }
        if (anim.runtimeAnimatorController == null || anim.runtimeAnimatorController.animationClips.Length == 0) {
            statusMessage = "Error: Animator has no Clips!"; statusType = MessageType.Warning; return;
        }
        Camera cam = GameObject.Find("BakingCam")?.GetComponent<Camera>();
        if (cam == null) {
            statusMessage = "Critical Error: 'BakingCam' is missing!"; statusType = MessageType.Error; return;
        }

        isValidSelection = true;
        statusMessage = $"READY: {obj.name} ({anim.runtimeAnimatorController.animationClips.Length} clips)";
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
        
        GUILayout.Label("1. ANIMATION CONFIGURATION", EditorStyles.boldLabel);
        GUILayout.BeginVertical("box");
        targetFPS = EditorGUILayout.Slider("FPS (Smoothness)", targetFPS, 1, 60);
        EditorGUILayout.LabelField("Storage size:", $"{(targetFPS/30f)*100:F0}% compared to original", EditorStyles.miniLabel);
        GUILayout.Space(5);
        useManualPosition = EditorGUILayout.ToggleLeft("Keep Original Position", useManualPosition);
        if (!useManualPosition) yOffset = EditorGUILayout.FloatField("Lower Unit (Y)", yOffset);
        vfxThreshold = EditorGUILayout.Slider("VFX Outline Threshold", vfxThreshold, 0f, 0.5f);
        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.Label("2. OUTPUT CONFIGURATION", EditorStyles.boldLabel);
        GUILayout.BeginVertical("box");
        targetSize = (OutputSize)EditorGUILayout.EnumPopup("Texture Size:", targetSize);
        compression = (CompressionType)EditorGUILayout.EnumPopup("Compression:", compression);
        
        GUILayout.Space(5);
        exportIndividualPNGs = EditorGUILayout.ToggleLeft("Export individual PNG frames (Debug)", exportIndividualPNGs);
        if (exportIndividualPNGs) 
        {
            EditorGUILayout.HelpBox("Will create a folder 'Assets/BakedFrames/...' containing images.", MessageType.Warning);
        }
        GUILayout.EndVertical();

        GUILayout.Space(20);

        EditorGUI.BeginDisabledGroup(!isValidSelection);
        if (GUILayout.Button("BAKE & CREATE DATA NOW", GUILayout.Height(50)))
        {
            BakeAndPackage();
        }
        EditorGUI.EndDisabledGroup();
    }

    void BakeAndPackage()
    {
        GameObject selected = Selection.activeGameObject;
        string selectedName = selected != null ? selected.name : "None";
        selectedName = selectedName.Replace(" ", "_").ToLowerInvariant(); // Sanitize name for folder/files
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

        // --- CREATE PNG FOLDER IF NEEDED ---
        string pngFolderPath = $"Assets/BakedFrames/{selectedName}";
        if (exportIndividualPNGs)
        {
            if (Directory.Exists(pngFolderPath)) Directory.Delete(pngFolderPath, true);
            Directory.CreateDirectory(pngFolderPath);
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        int totalFrames = 0;
        foreach(var c in clips) totalFrames += Mathf.Max(1, Mathf.FloorToInt(c.length * targetFPS) + 1);

        Debug.Log($"Starting Bake: {totalFrames} frames. Size: {size}. Format: {compression}");

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
                info.speedModifier = 1.0f; // Default standard speed
                info.scale = 1.0f; // Default standard scale
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
                    
                    // 3. Process Pixels (Remove black outline)
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

                    // --- EXPORT PNG IF NEEDED ---
                    if (exportIndividualPNGs)
                    {
                        byte[] bytes = tempTex.EncodeToPNG();
                        File.WriteAllBytes($"{pngFolderPath}/{clip.name}_{f:D3}.png", bytes);
                    }

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

            // 5. Finalize & Create Assets
            // 5. Finalize & Create Assets
            textureArray.Apply(false, true); 
            
            // --- CREATE FOLDER STRUCTURE ---
            string baseFolderPath = "Assets/BakedData";
            if (!Directory.Exists(baseFolderPath)) Directory.CreateDirectory(baseFolderPath);
            
            // Target folder for this specific unit
            string targetFolderPath = $"{baseFolderPath}/{selectedName}";
            if (!Directory.Exists(targetFolderPath)) Directory.CreateDirectory(targetFolderPath);
            
            // Save Texture Array
            string texPath = $"{targetFolderPath}/{selectedName}_Array.asset";
            AssetDatabase.CreateAsset(textureArray, texPath);

            // --- CREATE MATERIAL AUTOMATICALLY ---
            string matPath = $"{targetFolderPath}/{selectedName}_Material.mat";
            Shader instancedShader = Shader.Find("Abel/Instanced/BakedTextureArray");
            if (instancedShader != null)
            {
                Material newMat = new Material(instancedShader);
                newMat.SetTexture("_MainTexArray", textureArray);
                newMat.enableInstancing = true; // Critical for performance
                AssetDatabase.CreateAsset(newMat, matPath);
            }
            else
            {
                Debug.LogWarning("Shader 'Abel/Instanced/BakedTextureArray' not found. Material creation skipped.");
            }

            // Save AnimData
            string dataPath = $"{targetFolderPath}/{selectedName}_Data.asset";
            UnitAnimData dataSO = ScriptableObject.CreateInstance<UnitAnimData>();
            dataSO.textureArray = textureArray;
            dataSO.animations = animDataList;
            
            AssetDatabase.CreateAsset(dataSO, dataPath);
            
            // Force Unity to save and recognize the newly created folders and files
            AssetDatabase.SaveAssets();

            // --- NAVIGATE PROJECT TAB TO FOLDER ---
            // Load the folder as a DefaultAsset to select it in the Project window
            UnityEngine.Object folderObj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(targetFolderPath);
            if (folderObj != null)
            {
                Selection.activeObject = folderObj;
                EditorGUIUtility.PingObject(folderObj);
            }
            else
            {
                // Fallback to selecting the data object if folder ping fails
                Selection.activeObject = dataSO;
                EditorGUIUtility.PingObject(dataSO);
            }
            
            string msg = "DONE!";
            if (exportIndividualPNGs) msg += $"\nExported frame images to: {pngFolderPath}";
            Debug.Log(msg);
            
            if (exportIndividualPNGs) EditorUtility.RevealInFinder(pngFolderPath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error: {ex.Message}");
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