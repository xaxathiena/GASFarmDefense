namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Base interface for all unit logic modules (Brains).
    /// Decouples the "What to do" from the "Unit" entity.
    /// </summary>
    public interface IUnitLogic
    {
        /// <summary>Called every frame to update unit behavior.</summary>
        void Tick(Minion minion, float dt);
        
        /// <summary>Called when the logic is first assigned to a unit.</summary>
        void OnEnter(Minion minion);
        
        /// <summary>Called when the logic is removed or unit is destroyed.</summary>
        void OnExit(Minion minion);
    }
}
