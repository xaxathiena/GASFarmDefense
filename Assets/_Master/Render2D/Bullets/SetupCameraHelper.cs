using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class SetupCameraHelper
{
    [MenuItem("Tools/Setup Top-Down Camera")]
    public static void SetupCamera()
    {
        GameObject camObj = GameObject.Find("Main Camera");
        if (camObj == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        // Set tag
        camObj.tag = "MainCamera";
        
        // Set position and rotation
        camObj.transform.position = new Vector3(0, 15, 0);
        camObj.transform.rotation = Quaternion.Euler(90, 0, 0);
        
        // Configure camera component
        Camera cam = camObj.GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 10;
        }
        
        Debug.Log("Camera configured for top-down view!");
    }
}
#endif
