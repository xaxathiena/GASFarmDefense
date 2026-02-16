using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class FolderToTextureArray : EditorWindow
{
    // --- INPUT ---
    private DefaultAsset sourceFolder;
    private string folderPath;
    
    // --- INFO ---
    private List<string> foundFiles = new List<string>();
    private int width = 0;
    private int height = 0;
    private bool sizesAreConsistent = true;

    // --- CONFIG ---
    public enum CompressionType 
    { 
        Uncompressed_RGBA32 = TextureFormat.RGBA32,
        PC_DXT5 = TextureFormat.DXT5, 
        Mobile_ASTC = TextureFormat.ASTC_6x6
    }
    private CompressionType compression = CompressionType.Uncompressed_RGBA32;

    // --- ANIMATION LIST ---
    private List<ManualAnimInfo> animConfigs = new List<ManualAnimInfo>();
    private Vector2 scrollPos;

    [System.Serializable]
    public class ManualAnimInfo
    {
        public string name = "New Anim";
        public int frameCount = 10;
        public float fps = 15;
        public bool loop = true;
        public float speed = 1.0f;
        
        public int startFrameDisplay = 0; 
    }

    [MenuItem("Tools/Abel/Folder To Texture Array")]
    static void Init()
    {
        FolderToTextureArray window = GetWindow<FolderToTextureArray>();
        window.titleContent = new GUIContent("Folder Importer");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("1. CHỌN FOLDER CHỨA ẢNH (PNG/JPG)", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField("Folder Ảnh:", sourceFolder, typeof(DefaultAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            ScanFolder();
        }

        if (foundFiles.Count > 0)
        {
            if (sizesAreConsistent)
                EditorGUILayout.HelpBox($"Đã tìm thấy: {foundFiles.Count} ảnh.\nKích thước: {width}x{height}", MessageType.Info);
            else
                EditorGUILayout.HelpBox($"CẢNH BÁO: Các ảnh không cùng kích thước!\nTool sẽ tự động Resize về {width}x{height}.", MessageType.Warning);
        }
        else if (sourceFolder != null)
        {
            EditorGUILayout.HelpBox("Folder trống hoặc không có file ảnh!", MessageType.Warning);
        }

        GUILayout.Space(10);
        GUILayout.Label("2. CẤU HÌNH ANIMATION", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Thêm Animation", GUILayout.Height(30)))
        {
            animConfigs.Add(new ManualAnimInfo() { name = "Anim_" + (animConfigs.Count + 1) });
        }
        if (GUILayout.Button("Xóa Hết", GUILayout.Height(30)))
        {
            animConfigs.Clear();
        }
        GUILayout.EndHorizontal();

        scrollPos = GUILayout.BeginScrollView(scrollPos, "box", GUILayout.Height(300));
        int currentStartFrame = 0;
        int totalFramesUsed = 0;

        for (int i = 0; i < animConfigs.Count; i++)
        {
            var anim = animConfigs[i];
            anim.startFrameDisplay = currentStartFrame;

            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"#{i+1}", GUILayout.Width(30));
            anim.name = EditorGUILayout.TextField(anim.name, GUILayout.Width(120));
            if (GUILayout.Button("X", GUILayout.Width(25))) { animConfigs.RemoveAt(i); break; }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Start: {anim.startFrameDisplay}", GUILayout.Width(80));
            GUILayout.Label("Count:", GUILayout.Width(45));
            anim.frameCount = EditorGUILayout.IntField(anim.frameCount, GUILayout.Width(50));
            GUILayout.Label("FPS:", GUILayout.Width(35));
            anim.fps = EditorGUILayout.FloatField(anim.fps, GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            anim.loop = EditorGUILayout.ToggleLeft("Loop", anim.loop, GUILayout.Width(60));
            GUILayout.Label("Speed:", GUILayout.Width(45));
            anim.speed = EditorGUILayout.FloatField(anim.speed, GUILayout.Width(40));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            currentStartFrame += anim.frameCount;
            totalFramesUsed += anim.frameCount;
        }
        GUILayout.EndScrollView();

        GUILayout.Space(10);
        int remaining = foundFiles.Count - totalFramesUsed;
        if (remaining > 0) 
            EditorGUILayout.HelpBox($"Cảnh báo: Còn dư {remaining} ảnh chưa dùng.", MessageType.Warning);
        else if (remaining < 0)
            EditorGUILayout.HelpBox($"LỖI: Thiếu {Mathf.Abs(remaining)} ảnh!", MessageType.Error);

        GUILayout.Space(5);
        compression = (CompressionType)EditorGUILayout.EnumPopup("Chuẩn Nén:", compression);

        GUI.enabled = foundFiles.Count > 0 && remaining >= 0;
        if (GUILayout.Button("TẠO TEXTURE ARRAY & DATA", GUILayout.Height(50)))
        {
            Build();
        }
        GUI.enabled = true;
    }

    void ScanFolder()
    {
        foundFiles.Clear();
        width = 0; height = 0;
        sizesAreConsistent = true;

        if (sourceFolder == null) return;

        folderPath = AssetDatabase.GetAssetPath(sourceFolder);
        // Quét file ảnh (bỏ qua file .meta)
        var extensions = new string[] { ".png", ".jpg", ".jpeg", ".tga" };
        var info = new DirectoryInfo(folderPath);
        var files = info.GetFiles().Where(f => extensions.Contains(f.Extension.ToLower())).OrderBy(f => f.Name).ToArray();

        foreach (var f in files) foundFiles.Add(f.FullName);

        if (foundFiles.Count > 0)
        {
            // Đọc ảnh đầu tiên để lấy size chuẩn
            Texture2D tex = new Texture2D(2, 2);
            byte[] bytes = File.ReadAllBytes(foundFiles[0]);
            tex.LoadImage(bytes);
            width = tex.width;
            height = tex.height;
            DestroyImmediate(tex);
        }
    }

    void Build()
    {
        if (foundFiles.Count == 0) return;

        Texture2DArray textureArray = new Texture2DArray(width, height, foundFiles.Count, (TextureFormat)compression, false);
        textureArray.filterMode = FilterMode.Bilinear;
        textureArray.wrapMode = TextureWrapMode.Clamp;

        try
        {
            for (int i = 0; i < foundFiles.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Importing...", Path.GetFileName(foundFiles[i]), (float)i / foundFiles.Count)) break;

                Texture2D temp = new Texture2D(2, 2);
                byte[] bytes = File.ReadAllBytes(foundFiles[i]);
                temp.LoadImage(bytes);

                // --- FIX LỖI TEXTURESCALE: Tự Resize bằng RenderTexture ---
                if (temp.width != width || temp.height != height)
                {
                    Debug.LogWarning($"Resize ảnh {i}: {temp.width}x{temp.height} -> {width}x{height}");
                    Texture2D resized = ResizeTexture(temp, width, height);
                    DestroyImmediate(temp);
                    temp = resized;
                }
                // -----------------------------------------------------------

                if (compression != CompressionType.Uncompressed_RGBA32)
                    EditorUtility.CompressTexture(temp, (TextureFormat)compression, TextureCompressionQuality.Best);
                
                Graphics.CopyTexture(temp, 0, 0, textureArray, i, 0);
                DestroyImmediate(temp);
            }

            textureArray.Apply(false, true);
            string savePath = folderPath + "/" + sourceFolder.name + "_Array.asset";
            AssetDatabase.CreateAsset(textureArray, savePath);

            UnitAnimData dataSO = ScriptableObject.CreateInstance<UnitAnimData>();
            dataSO.textureArray = textureArray;
            dataSO.animations = new List<UnitAnimData.AnimInfo>();

            int currentStart = 0;
            foreach (var cfg in animConfigs)
            {
                UnitAnimData.AnimInfo info = new UnitAnimData.AnimInfo();
                info.animName = cfg.name;
                info.startFrame = currentStart;
                info.frameCount = cfg.frameCount;
                info.fps = cfg.fps;
                info.loop = cfg.loop;
                info.speedModifier = cfg.speed;
                info.duration = cfg.frameCount / cfg.fps;

                dataSO.animations.Add(info);
                currentStart += cfg.frameCount;
            }

            string dataPath = folderPath + "/" + sourceFolder.name + "_Data.asset";
            AssetDatabase.CreateAsset(dataSO, dataPath);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = dataSO;
            Debug.Log("Tạo thành công!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Lỗi: " + ex.Message);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // --- HÀM THAY THẾ TEXTURESCALE ---
    Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}