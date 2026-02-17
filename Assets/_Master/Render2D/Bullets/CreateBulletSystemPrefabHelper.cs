using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class CreateBulletSystemPrefabHelper
{
    [MenuItem("Tools/Create BulletSystem Prefab")]
    public static void CreatePrefab()
    {
        // Find the BulletSystem GameObject
        GameObject bsObj = GameObject.Find("BP_BulletSystem");
        if (bsObj == null)
        {
            Debug.LogError("BP_BulletSystem not found!");
            return;
        }

        // Create prefab
        string prefabPath = "Assets/_Master/Render2D/Bullets/BP_BulletSystem.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bsObj, prefabPath);
        
        if (prefab != null)
        {
            Debug.Log("Prefab created at: " + prefabPath);
            
            // Now wire it to GameLifetimeScope
            GameObject scopeObj = GameObject.Find("GameLifetimeScope");
            if (scopeObj != null)
            {
                BulletGameLifetimeScope scope = scopeObj.GetComponent<BulletGameLifetimeScope>();
                if (scope != null)
                {
                    // Use reflection to set the private field
                    var field = typeof(BulletGameLifetimeScope).GetField("bulletSystemPrefab", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (field != null)
                    {
                        BulletSystem bsPrefab = prefab.GetComponent<BulletSystem>();
                        field.SetValue(scope, bsPrefab);
                        EditorUtility.SetDirty(scope);
                        Debug.Log("Wired prefab to GameLifetimeScope!");
                    }
                    else
                    {
                        Debug.LogError("Could not find bulletSystemPrefab field!");
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Failed to create prefab!");
        }
    }
}
#endif
