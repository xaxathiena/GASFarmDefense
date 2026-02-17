using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class SetupBulletSystemHelper
{
    [MenuItem("Tools/Setup BulletSystem")]
    public static void SetupBulletSystem()
    {
        // Find the BulletSystem GameObject
        GameObject bsObj = GameObject.Find("BP_BulletSystem");
        if (bsObj == null)
        {
            Debug.LogError("BP_BulletSystem not found!");
            return;
        }

        BulletSystem bs = bsObj.GetComponent<BulletSystem>();
        if (bs == null)
        {
            Debug.LogError("BulletSystem component not found!");
            return;
        }

        // Create or load material
        string matPath = "Assets/_Master/Render2D/Bullets/InstancedUnit_Smooth.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        
        if (mat == null)
        {
            // Create new material
            Shader shader = Shader.Find("Unlit/InstancedUnitShader");
            if (shader == null)
            {
                shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Master/Render2D/InstancedUnitShader.shader");
            }
            
            if (shader != null)
            {
                mat = new Material(shader);
                mat.enableInstancing = true;
                AssetDatabase.CreateAsset(mat, matPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Created material: " + matPath);
            }
            else
            {
                Debug.LogError("Shader not found!");
                return;
            }
        }

        // Assign to BulletSystem
        bs.bulletMaterial = mat;
        
        // Assign Quad mesh (Unity's built-in)
        bs.bulletMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        
        // Assign UnitAnimData
        UnitAnimData bulletData = AssetDatabase.LoadAssetAtPath<UnitAnimData>("Assets/_Master/Render2D/Bullets/Bullets_Data.asset");
        if (bulletData != null)
        {
            bs.bulletData = bulletData;
            Debug.Log("Assigned Bullets_Data!");
        }
        else
        {
            Debug.LogWarning("Bullets_Data.asset not found!");
        }

        EditorUtility.SetDirty(bs);
        Debug.Log("BulletSystem configured!");
    }
}
#endif
