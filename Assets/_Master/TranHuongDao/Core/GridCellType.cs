namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Represents the logical state of a single cell in the tower-defense grid.
    /// Stored as a plain enum in a flat array — no GameObjects are allocated per cell.
    /// </summary>
    public enum GridCellType
    {
        /// <summary>Cell exists but has no designated role yet.</summary>
        Empty,

        /// <summary>A tower can be placed here.</summary>
        Buildable,

        /// <summary>Part of the enemy movement path; building is not allowed.</summary>
        Path,

        /// <summary>Permanently non-buildable (e.g., water, decorations, out-of-bounds).</summary>
        Blocked,

        /// <summary>A tower currently occupies this cell.</summary>
        TowerOccupied,
    }
}
