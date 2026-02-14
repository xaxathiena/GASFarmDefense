using UnityEngine;
using UnityEditor;

public class CreateBakingRT
{
    [MenuItem("Tools/Create Baking Render Texture")]
    public static void CreateRenderTexture()
    {
        RenderTexture rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        rt.name = "BakingRT";
        
        AssetDatabase.CreateAsset(rt, "Assets/BakingRT.renderTexture");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("BakingRT created successfully at Assets/BakingRT.renderTexture");
    }
}