using Abel.TranHuongDao.Core;
using Cysharp.Threading.Tasks;
using GASFarmDefense.Modules.Addressables;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

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
            await _uiManager.ViewManager.SwitchView<LoadingScreenView>();
            _uiManager.SetGlobalUIActive(false);

            // 2. Load Scene via Addressables
            SceneInstance sceneInstance = await _addressableService.LoadSceneAsync(map.MapScene);

            // The MapUISetupProvider will start automatically and show the Map HUD in the same ViewContainer.
            // To prevent the new Map HUD from rendering on top of the Loading Screen, we bring the Loading Screen to the front.
            var loadingScreen = _uiManager.ViewManager.GetView<LoadingScreenView>();
            if (loadingScreen != null)
            {
                loadingScreen.RootElement.BringToFront();
            }

            // 3. Keep Loading Screen visible for a short bit
            await UniTask.Delay(500); // Small buffer for visual comfort

            // 4. Hide the global Loading Screen
            await _uiManager.ViewManager.HideCurrentView();

            Debug.Log($"[SceneLoader] Map {map.DisplayName} loaded successfully.");
        }

        public async UniTask ReturnToHome(string homeSceneName = "MainScene")
        {
            // 1. Show Loading Screen
            await _uiManager.ViewManager.SwitchView<LoadingScreenView>();

            // 2. Cleanup Map UI
            // Map UI is now handled by scene scope lifetime
            // _uiManager.CleanupMapUI();

            // 3. Load Home Scene

            AsyncOperation loadOp = SceneManager.LoadSceneAsync(homeSceneName, LoadSceneMode.Single);
            await loadOp.ToUniTask();

            // 4. Restore Global UI
            _uiManager.SetGlobalUIActive(true);
            await _uiManager.ViewManager.SwitchView<MainMenuView>();

            Debug.Log("[SceneLoader] Returned to Home.");
        }
    }
}
