using UnityEngine;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// ScriptableObject asset that wraps <see cref="TowerBuilderConfig"/>.
    /// Create one asset via the Unity menu, assign it in the Inspector on
    /// <see cref="GameLifetimeScope"/>, then VContainer injects it as a singleton.
    /// </summary>
    [CreateAssetMenu(
        fileName = "TowerBuilderConfigSO",
        menuName  = "Abel/TowerDefense/Tower Builder Config")]
    public class TowerBuilderConfigSO : ScriptableObject
    {
        // Embedded as a plain serializable struct — no hidden MonoBehaviour state.
        public TowerBuilderConfig config;
    }
}
