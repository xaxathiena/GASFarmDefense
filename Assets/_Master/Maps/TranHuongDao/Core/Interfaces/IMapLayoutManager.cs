using System.Collections.Generic;
using UnityEngine;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Single map contract: grid layout, coordinate conversion, cell state,
    /// build validation, and enemy path data.
    ///
    /// Designed to be consumed by:
    ///   • TowerDragDropManager   (CanBuildAt, SetCellState, WorldToGridPosition)
    ///   • GAS Build Tower ability (CanBuildAt, SetCellState)
    ///   • EnemyManager / WaveManager (GetEnemyPath, GetPaths)
    ///   • UnitRender system          (GridToWorldPosition)
    /// </summary>
    public interface IMapLayoutManager
    {
        // ── Grid metadata ─────────────────────────────────────────────────────
        int GridWidth { get; }
        int GridHeight { get; }
        float CellSize { get; }

        // ── Coordinate conversion ─────────────────────────────────────────────

        /// <summary>
        /// Returns the world-space center of the cell at (<paramref name="x"/>, <paramref name="y"/>).
        /// </summary>
        Vector3 GridToWorldPosition(int x, int y);

        /// <summary>
        /// Returns the grid indices that correspond to <paramref name="worldPosition"/>.
        /// The result is clamped to valid grid bounds.
        /// </summary>
        Vector2Int WorldToGridPosition(Vector3 worldPosition);

        // ── Cell state ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the current <see cref="GridCellType"/> of the cell at <paramref name="gridPos"/>.
        /// Returns <see cref="GridCellType.Blocked"/> for out-of-bounds coordinates.
        /// </summary>
        GridCellType GetCellState(Vector2Int gridPos);

        /// <summary>
        /// Overwrites the state of the cell at <paramref name="gridPos"/>.
        /// Out-of-bounds writes are silently ignored.
        /// </summary>
        void SetCellState(Vector2Int gridPos, GridCellType state);

        // ── GAS / Build query ─────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when a tower may be placed at <paramref name="worldPosition"/>
        /// (the underlying cell must be <see cref="GridCellType.Buildable"/>).
        /// Safe to call before the grid is initialized — returns <c>false</c>.
        /// </summary>
        bool CanBuildAt(Vector3 worldPosition);

        // ── Path query ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the ordered world-space waypoints that enemies follow on a single lane.
        /// </summary>
        IReadOnlyList<Vector3>[] GetEnemyPath();
    }
}
