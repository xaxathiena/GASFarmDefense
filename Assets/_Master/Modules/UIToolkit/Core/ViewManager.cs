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

        public async UniTask SwitchView<T>() where T : ViewBase
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
            }
            else
            {
                UnityEngine.Debug.LogError($"[ViewManager] View of type {type.Name} is not registered!");
            }
        }
    }
}
