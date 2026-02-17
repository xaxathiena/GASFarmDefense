using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class SetupGunHelper
{
    [MenuItem("Tools/Setup Gun")]
    public static void SetupGun()
    {
        // Find GameObjects
        GameObject turretObj = GameObject.Find("TurretPivot");
        GameObject muzzleObj = GameObject.Find("MuzzlePoint");
        
        if (turretObj == null || muzzleObj == null)
        {
            Debug.LogError("TurretPivot or MuzzlePoint not found!");
            return;
        }

        // Set MuzzlePoint as child of TurretPivot
        muzzleObj.transform.SetParent(turretObj.transform);
        
        // Position TurretPivot above ground
        turretObj.transform.position = new Vector3(0, 0, 0);
        turretObj.transform.localScale = new Vector3(0.5f, 1, 0.5f);
        
        // Position MuzzlePoint at the front of the cylinder (barrel tip)
        muzzleObj.transform.localPosition = new Vector3(0, 1, 0); // At the top of cylinder
        
        // Configure GunController
        GunController gun = turretObj.GetComponent<GunController>();
        if (gun != null)
        {
            var turretField = typeof(GunController).GetField("turretPivot", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var muzzleField = typeof(GunController).GetField("muzzlePoint", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (turretField != null && muzzleField != null)
            {
                turretField.SetValue(gun, turretObj.transform);
                muzzleField.SetValue(gun, muzzleObj.transform);
                EditorUtility.SetDirty(gun);
                Debug.Log("Gun configured successfully!");
            }
            else
            {
                Debug.LogError("Could not find turretPivot or muzzlePoint fields!");
            }
        }
        else
        {
            Debug.LogError("GunController component not found!");
        }
    }
}
#endif
