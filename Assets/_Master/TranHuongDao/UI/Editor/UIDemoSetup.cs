#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using GASFarmDefense.UIToolkit.TranHuongDao;

namespace GASFarmDefense.UIToolkit.Editor
{
    public class UIDemoSetup
    {
        [MenuItem("Tools/Setup UI Toolkit Demo Scene")]
        public static void SetupScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            var go = new GameObject("GameUIManager");
            var uiDoc = go.AddComponent<UIDocument>();
            var manager = go.AddComponent<GameUIManager>();
            
            // Load UXMLs (matching paths generated previously)
            var background = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Master/TranHuongDao/UI/Background/GlobalBackground.uxml");
            var topBarAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Master/TranHuongDao/UI/TopBar/GlobalTopBar.uxml");
            var mainMenu = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Master/TranHuongDao/UI/MainMenuView/MainMenuView.uxml");
            var mapSel = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Master/TranHuongDao/UI/MapSelectionView/MapSelectionView.uxml");
            var loading = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Master/TranHuongDao/UI/LoadingScreenView/LoadingScreenView.uxml");
            
            var serializedObject = new SerializedObject(manager);
            serializedObject.FindProperty("_backgroundUxml").objectReferenceValue = background;
            serializedObject.FindProperty("_topBarUxml").objectReferenceValue = topBarAsset;
            serializedObject.FindProperty("_mainMenuUxml").objectReferenceValue = mainMenu;
            serializedObject.FindProperty("_mapSelectionUxml").objectReferenceValue = mapSel;
            serializedObject.FindProperty("_loadingScreenUxml").objectReferenceValue = loading;
            serializedObject.ApplyModifiedProperties();
            
            // Create a temporary panel settings if there isn't one globally (standard procedure helps visuals)
            // But leaving it default is usually fine in Unity 2021+.

            if (mainMenu == null) Debug.LogWarning("MainMenuView UXML not found at path!");
            
            EditorSceneManager.SaveScene(scene, "Assets/_Master/TranHuongDao/UI/UIToolkit_Demo.unity");
            Debug.Log("UI Demo Scene created and saved successfully! Press Play to view the flow.");
        }
    }
}
#endif
