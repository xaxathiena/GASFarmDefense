using Unity.Mathematics;
using Abel.TowerDefense.Config;
using Abel.TowerDefense.DebugTools;

namespace Abel.TowerDefense.Core
{
    /// <summary>
    /// Standard interface for any entity in the logic system that needs to be rendered.
    /// Extends IUnitDebugInfo to support debugging automatically.
    /// </summary>
    public interface ILogicEntity : IUnitDebugInfo
    {
        string UnitID { get; }
        float2 Position { get; }
        float Rotation { get; }
        float Scale { get; }
        UnitState CurrentState { get; }
        float PlaySpeed { get; }
    }
}