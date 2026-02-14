using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class TextureArrayPacker : EditorWindow
{
    [MenuItem("Tools/Abel/Pack Baked Frames to Texture2DArray")]
    public static void PackBakedFrames()
    {
        // Get the selected folder in Project view
        string selectedPath = GetSelectedPathOrFallback();
        
        if (string.IsNullOrEmpty(selectedPath))
        {
            EditorUtility.DisplayDialog("Error", 
                "Please select a unit folder in Assets/BakedSpine/[UnitName] first!", "OK");
            return;
        }

        // Validate folder path
        if (!selectedPath.Contains("BakedSpine"))
        {
            EditorUtility.DisplayDialog("Error", 
                "Selected folder must be inside Assets/BakedSpine/[UnitName]!", "OK");
            return;
        }

        PackFolder(selectedPath);
    }

    private static string GetSelectedPathOrFallback()
    {
        string path = "Assets";
        
        foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
        {
            path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                return path;
            }
        }
        
        return null;
    }

    public static void PackFolder(string folderPath)
    {
        // 1. LOAD ALL PNG FILES
        string[] pngFiles = Directory.GetFiles(folderPath, "*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToArray();

        if (pngFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", 
                $"No PNG files found in folder:\n{folderPath}", "OK");
            return;
        }

        Debug.Log($"Found {pngFiles.Length} PNG files to pack");

        // 2. LOAD FIRST IMAGE TO GET DIMENSIONS
        Texture2D firstTex = LoadTexture(pngFiles[0]);
        if (firstTex == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to load first texture!", "OK");
            return;
        }

        int width = firstTex.width;
        int height = firstTex.height;
        TextureFormat format = TextureFormat.RGBA32;

        Debug.Log($"Texture dimensions: {width}x{height}, Format: {format}");

        // 3. CREATE TEXTURE2DARRAY
        Texture2DArray textureArray = new Texture2DArray(
            width, 
            height, 
            pngFiles.Length, 
            format, 
            true, // mipChain
            false // linear
        );
        
        textureArray.filterMode = FilterMode.Bilinear;
        textureArray.wrapMode = TextureWrapMode.Clamp;

        // 4. COPY TEXTURES INTO ARRAY
        Dictionary<string, List<int>> animationFrameMap = new Dictionary<string, List<int>>();
        
        for (int i = 0; i < pngFiles.Length; i++)
        {
            if (i % 50 == 0)
            {
                EditorUtility.DisplayProgressBar("Packing Textures", 
                    $"Processing frame {i}/{pngFiles.Length}", 
                    (float)i / pngFiles.Length);
            }

            Texture2D tex = (i == 0) ? firstTex : LoadTexture(pngFiles[i]);
            
            if (tex == null)
            {
                Debug.LogWarning($"Failed to load texture: {pngFiles[i]}");
                continue;
            }

            // Validate dimensions
            if (tex.width != width || tex.height != height)
            {
                Debug.LogWarning($"Texture {i} has different dimensions ({tex.width}x{tex.height}), skipping!");
                if (i > 0) DestroyImmediate(tex);
                continue;
            }

            // Copy texture to array slice
            Graphics.CopyTexture(tex, 0, 0, textureArray, i, 0);
            
            // Track animation name from filename
            string fileName = Path.GetFileNameWithoutExtension(pngFiles[i]);
            string animName = ExtractAnimationName(fileName);
            
            if (!animationFrameMap.ContainsKey(animName))
            {
                animationFrameMap[animName] = new List<int>();
            }
            animationFrameMap[animName].Add(i);

            // Clean up
            if (i > 0) DestroyImmediate(tex);
        }

        textureArray.Apply(updateMipmaps: true, makeNoLongerReadable: false);

        // 5. SAVE TEXTURE2DARRAY ASSET
        string unitName = Path.GetFileName(folderPath);
        string assetPath = $"{folderPath}/{unitName}_TextureArray.asset";
        
        AssetDatabase.CreateAsset(textureArray, assetPath);
        Debug.Log($"Saved Texture2DArray to: {assetPath}");

        // 6. CREATE ANIMATION FRAME DATA
        AnimationFrameData frameData = ScriptableObject.CreateInstance<AnimationFrameData>();
        frameData.textureArray = textureArray;
        
        foreach (var kvp in animationFrameMap.OrderBy(k => k.Value.First()))
        {
            string animName = kvp.Key;
            List<int> frames = kvp.Value;
            
            if (frames.Count > 0)
            {
                int startFrame = frames.First();
                int endFrame = frames.Last();
                frameData.animations.Add(new AnimationClipInfo(animName, startFrame, endFrame));
            }
        }

        string frameDataPath = $"{folderPath}/{unitName}_FrameData.asset";
        AssetDatabase.CreateAsset(frameData, frameDataPath);
        Debug.Log($"Saved Animation Frame Data to: {frameDataPath}");

        // 7. CREATE TEXT LOG FILE
        string logPath = $"{folderPath}/{unitName}_FrameLog.txt";
        File.WriteAllText(logPath, frameData.GetSummary());
        Debug.Log($"Saved frame log to: {logPath}");

        // Cleanup and refresh
        DestroyImmediate(firstTex);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();

        // Select the created assets
        Selection.activeObject = frameData;
        EditorGUIUtility.PingObject(frameData);

        EditorUtility.DisplayDialog("Success!", 
            $"Packed {pngFiles.Length} frames into Texture2DArray!\n\n{frameData.GetSummary()}", "OK");
    }

    private static Texture2D LoadTexture(string path)
    {
        byte[] fileData = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(fileData);
        return tex;
    }

    private static string ExtractAnimationName(string fileName)
    {
        // Extract animation name from filename like "Idle_0136_0001"
        // Returns "Idle_0136"
        int lastUnderscore = fileName.LastIndexOf('_');
        if (lastUnderscore > 0)
        {
            return fileName.Substring(0, lastUnderscore);
        }
        return fileName;
    }
}
