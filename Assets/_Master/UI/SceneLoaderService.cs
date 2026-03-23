using Abel.TranHuongDao.Core;
using Cysharp.Threading.Tasks;
using GASFarmDefense.Modules.Addressables;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.TranHuongDao
{
    public class SceneLoaderService
    {
        private readonly GameUIManager _uiManager;
        private readonly IAddressableService _addressableService;
        private FD_MapConfigSO _currentMap;

        public SceneLoaderService(GameUIManager uiManager, IAddressableService addressableService)
        {
            _uiManager = uiManager;
            _addressableService = addressableService;
        }

        public async UniTask LoadMap(FD_MapConfigSO map)
        {
            _currentMap = map;

            // 1. Show Loading Screen
            var loadingScreen = await _uiManager.ViewManager.SwitchView<LoadingScreenView>();
            _uiManager.SetGlobalUIActive(false);
            if (loadingScreen != null) loadingScreen.RootElement.BringToFront();

            // 2. Load Scene via Addressables
            SceneInstance sceneInstance = await _addressableService.LoadSceneAsync(map.MapScene);

            // Re-check loading screen after scene load to ensure it stays on top of new Map HUD
            loadingScreen = _uiManager.ViewManager.GetView<LoadingScreenView>();
            if (loadingScreen != null)
            {
                loadingScreen.RootElement.BringToFront();
                // Ensure it's visible and blocking
                loadingScreen.RootElement.style.display = DisplayStyle.Flex;
            }

            // 3. Keep Loading Screen visible for a short bit
            await UniTask.Delay(500); // Small buffer for visual comfort

            // 4. Hide the global Loading Screen
            await _uiManager.ViewManager.HideView<LoadingScreenView>();

            Debug.Log($"[SceneLoader] Map {map.DisplayName} loaded successfully.");
        }

        public async UniTask ReturnToHome(string emptySceneName = "EmptyScene")
        {
            // 1. Show Loading Screen
            await _uiManager.ViewManager.SwitchView<LoadingScreenView>();

            // 2. Load Empty Scene (Single mode unloads the map)
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(emptySceneName, LoadSceneMode.Single);
            if (loadOp == null)
            {
                Debug.LogError($"[SceneLoader] Scene '{emptySceneName}' could not be loaded. Please ensure it is added to the Build Settings.");
                // Fallback attempt to restore UI even if scene load fails (though highly unlikely to work if map is still there)
            }
            else
            {
                await loadOp.ToUniTask();
            }

            // 3. Restore Global UI
            _uiManager.SetGlobalUIActive(true);
            await _uiManager.ViewManager.SwitchView<MainMenuView>();

            Debug.Log("[SceneLoader] Returned to Home via Empty Scene.");
        }
    }
}
