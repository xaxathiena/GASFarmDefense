using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.Core
{
    public class PopupManager
    {
        private Dictionary<Type, PopupBase> _popupDict = new Dictionary<Type, PopupBase>();
        private Stack<PopupBase> _activePopups = new Stack<PopupBase>();
        private VisualElement _popupContainer;

        public void Init(VisualElement popupContainer)
        {
            _popupContainer = popupContainer;
        }

        public void RegisterPopup<T>(T popup) where T : PopupBase
        {
            var type = typeof(T);
            _popupContainer.Add(popup.RootElement);
            _popupDict[type] = popup;
            
            popup.OnCloseRequested += () => HideTopPopup().Forget();
        }

        public async UniTask ShowPopup<T>() where T : PopupBase
        {
            var type = typeof(T);

            if (_popupDict.TryGetValue(type, out PopupBase popup))
            {
                // Bring to front in the visual tree (render on top of others)
                popup.RootElement.BringToFront();
                _activePopups.Push(popup);

                // Enable container picking if it was disabled
                _popupContainer.pickingMode = PickingMode.Position;

                await popup.Show();
            }
            else
            {
                UnityEngine.Debug.LogError($"[PopupManager] Popup of type {type.Name} is not registered!");
            }
        }

        public async UniTask HideTopPopup()
        {
            if (_activePopups.Count > 0)
            {
                PopupBase popup = _activePopups.Pop();
                await popup.Hide();

                // If no popups are left, make container ignore pointer events so background views can be clicked
                if (_activePopups.Count == 0)
                {
                    _popupContainer.pickingMode = PickingMode.Ignore;
                }
            }
        }
    }
}
