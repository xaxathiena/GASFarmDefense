using Abel.GAS.Samples.OrbCombat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Abel.GAS.Samples.OrbCombat.Editor
{
    [InitializeOnLoad]
    public class OrbCombatSetup
    {
        static OrbCombatSetup()
        {
            // Log này sẽ hiện ra ngay khi Unity compile xong
            Debug.Log("Checking for GAS Sample Menu...");
        }

        [MenuItem("Tools/Abel/GAS Samples/Setup Orb Combat Scene")]
        public static void SetupScene()
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            newScene.name = "OrbCombatTest";

            GameObject manager = new GameObject("OrbCombatManager");
            var scope = manager.AddComponent<OrbCombatLifetimeScope>();
            var bootstrap = manager.AddComponent<OrbCombatBootstrap>();
            
            var so = new SerializedObject(bootstrap);
            var prop = so.FindProperty("_lifetimeScope");
            if (prop != null)
            {
                prop.objectReferenceValue = scope;
                so.ApplyModifiedProperties();
            }
            
            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(0, 0, -10);
                Camera.main.transform.LookAt(Vector3.zero);
            }

            Debug.Log("✅ [GAS Sample] Đã khởi tạo Scene Orb Combat thành công!");
        }
    }
}
