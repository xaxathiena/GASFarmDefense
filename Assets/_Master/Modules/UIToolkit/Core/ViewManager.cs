using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace GASFarmDefense.UIToolkit.Core
{
    public class ViewManager
    {
        private Dictionary<Type, ViewBase> _viewDict = new Dictionary<Type, ViewBase>();
        private ViewBase _currentView;
        private VisualElement _viewContainer;

        public void Init(VisualElement viewContainer)
        {
            _viewContainer = viewContainer;
        }

        public void RegisterView<T>(T view) where T : ViewBase
        {
            var type = typeof(T);
            _viewContainer.Add(view.RootElement);
            _viewDict[type] = view;
        }

        public async UniTask<T> SwitchView<T>() where T : ViewBase
        {
            var type = typeof(T);

            // 1. If there's a current view, hide it and wait
            if (_currentView != null)
            {
                await _currentView.Hide();
            }

            // 2. Switch to new view
            if (_viewDict.TryGetValue(type, out ViewBase nextView))
            {
                _currentView = nextView;
                await _currentView.Show();
                return (T)_currentView;
            }
            else
            {
                UnityEngine.Debug.LogError($"[ViewManager] View of type {type.Name} is not registered!");
                return null;
            }
        }

        public async UniTask HideCurrentView()
        {
            if (_currentView != null)
            {
                await _currentView.Hide();
                _currentView = null;
            }
        }

        public async UniTask HideView<T>() where T : ViewBase
        {
            var type = typeof(T);
            if (_viewDict.TryGetValue(type, out ViewBase view))
            {
                if (_currentView == view)
                {
                    await HideCurrentView();
                }
                else
                {
                    await view.Hide();
                }
            }
        }

        public T GetView<T>() where T : ViewBase
        {
            if (_viewDict.TryGetValue(typeof(T), out ViewBase view))
            {
                return (T)view;
            }
            return null;
        }

        public void UnregisterView<T>() where T : ViewBase
        {
            var type = typeof(T);
            if (_viewDict.TryGetValue(type, out ViewBase view))
            {
                _viewContainer.Remove(view.RootElement);
                _viewDict.Remove(type);
                if (_currentView == view) _currentView = null;
            }
        }

        public void ClearViews(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                if (_viewDict.TryGetValue(type, out ViewBase view))
                {
                    _viewContainer.Remove(view.RootElement);
                    _viewDict.Remove(type);
                    if (_currentView == view) _currentView = null;
                }
            }
        }
    }
}
