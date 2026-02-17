using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class SetupGroundHelper
{
    [MenuItem("Tools/Setup Ground")]
    public static void SetupGround()
    {
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            Debug.LogError("Ground not found!");
            return;
        }

        // Scale to make it a floor (large and flat)
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(50, 0.1f, 50);
        
        Debug.Log("Ground configured!");
    }
}
#endif
