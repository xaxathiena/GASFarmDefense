using UnityEngine;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Base class for all configuration ScriptableObjects.
    /// Ensures ConfigService can load and identify them automatically.
    /// </summary>
    public abstract class BaseConfigSO : ScriptableObject
    {
        /// <summary>
        /// Optional: Override this to pre-warm dictionaries or perform setup 
        /// right after ConfigService loads this asset at startup.
        /// </summary>
        public virtual void InitializeConfig() { }
    }
}