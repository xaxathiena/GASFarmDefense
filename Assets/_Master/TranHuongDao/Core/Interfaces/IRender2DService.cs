using UnityEngine;
using Abel.TowerDefense.Render; // UnitSyncData, GameRenderManager

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Thin service wrapper around the custom Render2D pipeline (GameRenderManager).
    /// Game logic talks to this interface; it never references GameRenderManager directly.
    /// </summary>
    public interface IRender2DService
    {
        // --- Unit Rendering ---

        /// <summary>
        /// Register and render a unit for the first time at <paramref name="position"/>.
        /// <paramref name="unitID"/> must exist in UnitRenderDatabase.
        /// </summary>
        void RenderUnit(string unitID, int instanceID, Vector3 position, float rotation = 0f, float scale = 1f);

        /// <summary>
        /// Push updated transform + animation data for an already-registered unit.
        /// Call every frame for moving entities (enemies).
        /// </summary>
        void UpdateRender(string unitID, int instanceID, Vector3 position, float rotation = 0f, float scale = 1f);

        /// <summary>
        /// Remove a unit from the render pipeline (death, despawn, sell).
        /// </summary>
        void RemoveRender(string unitID, int instanceID);

        /// <summary>
        /// Remove ALL rendered instances belonging to <paramref name="unitID"/>.
        /// Useful for clearing an entire wave group at once.
        /// </summary>
        void RemoveAllOfType(string unitID);

        // --- Animation ---

        /// <summary>
        /// Set the animation state for a specific instance.
        /// <paramref name="animIndex"/> maps to the index inside UnitAnimData.animations list.
        /// </summary>
        void SetAnimationState(string unitID, int instanceID, int animIndex, float playSpeed = 1f);
    }
}
