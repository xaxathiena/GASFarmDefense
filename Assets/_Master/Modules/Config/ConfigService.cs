using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Abel.TranHuongDao.Core
{
    public interface IConfigService
    {
        /// <summary>
        /// Retrieves a strongly-typed, read-only configuration object.
        /// </summary>
        T GetConfig<T>() where T : BaseConfigSO;
    }

    public class ConfigService : IConfigService, IInitializable
    {
        // Dictionary key is the exact Type of the Config (e.g., typeof(UnitsConfig))
        private readonly Dictionary<Type, BaseConfigSO> _configDict = new Dictionary<Type, BaseConfigSO>();

        public void Initialize()
        {
            // 1. Load all assets deriving from BaseConfigSO located in any "Resources/Configs" folder
            BaseConfigSO[] loadedConfigs = Resources.LoadAll<BaseConfigSO>("Configs");

            if (loadedConfigs.Length == 0)
            {
                Debug.LogWarning("[ConfigService] No config files found in Resources/Configs!");
                return;
            }

            // 2. Populate the dictionary mapped by Type
            foreach (var config in loadedConfigs)
            {
                Type configType = config.GetType();
                
                if (!_configDict.ContainsKey(configType))
                {
                    config.InitializeConfig(); // Trigger internal dictionary building
                    _configDict.Add(configType, config);
                    Debug.Log($"[ConfigService] Loaded config: {configType.Name}");
                }
                else
                {
                    Debug.LogError($"[ConfigService] Duplicate config type detected: {configType.Name}. Only one SO per type is allowed!");
                }
            }

            Debug.Log($"[ConfigService] Successfully loaded and cached {_configDict.Count} config modules.");
        }

        public T GetConfig<T>() where T : BaseConfigSO
        {
            if (_configDict.TryGetValue(typeof(T), out BaseConfigSO baseConfig))
            {
                // Safe cast back to the requested type
                return baseConfig as T;
            }

            Debug.LogError($"[ConfigService] Missing configuration requested for type: {typeof(T).Name}");
            return null;
        }
    }
}