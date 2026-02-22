using UnityEngine;
using VContainer;
using Unity.Collections;
using System.Collections.Generic;
using Abel.TowerDefense.Render;
using Abel.TowerDefense.Config;
using Abel.TowerDefense.DebugTools;

namespace Abel.TowerDefense.Core
{
    /// <summary>
    /// Base class for any logic system that needs to push data to the Render System.
    /// Handles Native buffers, DI injection, and Debugger hooks.
    /// </summary>
    public abstract class UnitLogicSystemBase : MonoBehaviour
    {
        protected GameRenderManager renderManager;
        protected UnitRenderDatabase database;
        protected UnitDebugger unitDebugger;

        // Protected dictionary so child classes can read/modify if absolutely necessary,
        // but prefer using AddEntity / RemoveEntity.
        protected Dictionary<int, ILogicEntity> activeEntities = new Dictionary<int, ILogicEntity>();
        
        // Internal buffers for pushing data to GPU
        private Dictionary<string, NativeList<UnitSyncData>> syncBuffers = new Dictionary<string, NativeList<UnitSyncData>>();

        [Inject]
        public virtual void Construct(GameRenderManager renderMgr, UnitRenderDatabase db, UnitDebugger debugger)
        {
            this.renderManager = renderMgr;
            this.database = db;
            this.unitDebugger = debugger;

            if (this.unitDebugger != null)
            {
                this.unitDebugger.onRequestLogicInfo += ProvideLogicInfo;
            }
        }

        /// <summary>
        /// Hook for the UnitDebugger. Returns null if this specific system doesn't own the ID.
        /// </summary>
        protected virtual string ProvideLogicInfo(int id)
        {
            if (activeEntities.TryGetValue(id, out var entity))
            {
                return entity.GetUnitInfo();
            }
            return null; // Let the debugger ask other systems
        }

        /// <summary>
        /// Registers a new entity to be rendered.
        /// </summary>
        protected void AddEntity(ILogicEntity entity)
        {
            activeEntities.Add(entity.GetInstanceID(), entity);
        }

        /// <summary>
        /// Unregisters an entity from rendering.
        /// </summary>
        protected void RemoveEntity(int instanceID)
        {
            activeEntities.Remove(instanceID);
        }

        /// <summary>
        /// Gathers all active entities, converts them to UnitSyncData, and pushes them to the RenderManager.
        /// Child classes must call this at the end of their Update/Tick loop.
        /// </summary>
        protected void PushDataToRenderSystem()
        {
            // 1. Clear all buffers
            foreach (var buffer in syncBuffers.Values)
            {
                buffer.Clear();
            }

            // 2. Populate buffers
            foreach (var entity in activeEntities.Values)
            {
                if (!syncBuffers.ContainsKey(entity.UnitID))
                {
                    syncBuffers.Add(entity.UnitID, new NativeList<UnitSyncData>(Allocator.Persistent));
                }

                var profileData = database.GetUnitByID(entity.UnitID);
                int currentAnimIndex = (profileData != null) ? profileData.GetAnimIndex(entity.CurrentState) : 0;

                syncBuffers[entity.UnitID].Add(new UnitSyncData
                {
                    instanceID = entity.GetInstanceID(),
                    position = entity.Position,
                    rotation = entity.Rotation,
                    scale = entity.Scale,
                    animIndex = currentAnimIndex,
                    playSpeed = entity.PlaySpeed
                });
            }

            // 3. Push to Render System
            foreach (var kvp in syncBuffers)
            {
                renderManager.PushDataToRender(kvp.Key, kvp.Value.AsArray(), kvp.Value.Length);
            }
        }

        protected virtual void OnDestroy()
        {
            if (unitDebugger != null)
            {
                unitDebugger.onRequestLogicInfo -= ProvideLogicInfo;
            }
            
            foreach (var buffer in syncBuffers.Values)
            {
                if (buffer.IsCreated) buffer.Dispose();
            }
        }
    }
}