namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// A centralized service responsible for generating unique, non-overlapping 
    /// Instance IDs for all game entities (Towers, Enemies, Projectiles, etc.).
    /// </summary>
    public interface IInstanceIDService
    {
        /// <summary>
        /// Returns a globally unique Instance ID and increments the internal counter.
        /// </summary>
        int GetNextID();
    }

    public class InstanceIDService : IInstanceIDService
    {
        // Start from 1. ID 0 can be reserved for "Invalid" or "Null" entity.
        private int _currentID = 1;

        public int GetNextID()
        {
            // The postfix increment (++) returns the current value, THEN increments it.
            // This ensures every call gets a unique number.
            return _currentID++;
        }
    }
}