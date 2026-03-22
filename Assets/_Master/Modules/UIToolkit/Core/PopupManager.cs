using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.Core
{
    public class PopupManager
    {
        private Dictionary<Type, PopupBase> _popupDict = new Dictionary<Type, PopupBase>();
        private Dictionary<string, PopupBase> _idPopupDict = new Dictionary<string, PopupBase>();
        private Stack<PopupBase> _activePopups = new Stack<PopupBase>();
        private VisualElement _popupContainer;

        public void Init(VisualElement popupContainer)
        {
            _popupContainer = popupContainer;
        }

        public void RegisterPopup<T>(T popup) where T : PopupBase
        {
            var type = typeof(T);
            RegisterPopupInternal(type.Name, popup);
            _popupDict[type] = popup;
        }

        public void RegisterPopup(string id, PopupBase popup)
        {
            RegisterPopupInternal(id, popup);
        }

        private void RegisterPopupInternal(string id, PopupBase popup)
        {
            if (!_popupContainer.Contains(popup.RootElement))
            {
                _popupContainer.Add(popup.RootElement);
            }
            _idPopupDict[id] = popup;
            popup.OnCloseRequested += () => HideTopPopup().Forget();
        }

        public async UniTask ShowPopup<T>() where T : PopupBase
        {
            await ShowPopup(typeof(T).Name);
        }

        public async UniTask ShowPopup(string id)
        {
            if (_idPopupDict.TryGetValue(id, out PopupBase popup))
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
                UnityEngine.Debug.LogError($"[PopupManager] Popup with ID '{id}' is not registered!");
            }
        }

        public void UnregisterPopup(string id)
        {
            if (_idPopupDict.TryGetValue(id, out PopupBase popup))
            {
                _popupContainer.Remove(popup.RootElement);
                _idPopupDict.Remove(id);

                // Also remove from type dict if it matches
                var keysToRemove = new List<Type>();
                foreach (var pair in _popupDict)
                {
                    if (pair.Value == popup) keysToRemove.Add(pair.Key);
                }
                foreach (var key in keysToRemove) _popupDict.Remove(key);
            }
        }

        public void ClearPopups(IEnumerable<string> ids)
        {
            foreach (var id in ids) UnregisterPopup(id);
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
