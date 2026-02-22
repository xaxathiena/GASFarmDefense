using System.Collections.Generic;
using UnityEngine;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Provides the waypoint paths used by the current map.
    /// Each element is one enemy lane (ordered list of world-space positions).
    /// </summary>
    public interface IMapManager
    {
        /// <summary>
        /// Returns all available paths on the current map.
        /// Index matches <see cref="SpawnEntry.pathIndex"/>.
        /// </summary>
        IReadOnlyList<Vector3>[] GetPaths();
    }
}
