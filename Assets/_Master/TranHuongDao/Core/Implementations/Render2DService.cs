using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using VContainer.Unity;
using Abel.TowerDefense.Render;
using Abel.TowerDefense.Config;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Bridges pure-C# game logic to the GPU-batched Render2D pipeline.
    ///
    /// Per unitID, maintains a slot-mapped <see cref="NativeArray{T}"/> of
    /// <see cref="UnitSyncData"/>.  Each frame, all dirty buffers are flushed
    /// once via <see cref="GameRenderManager.PushDataToRender"/> (ITickable).
    /// No LINQ; no per-frame allocations.
    /// </summary>
    public class Render2DService : IRender2DService, IInitializable, ITickable, System.IDisposable
    {
        // ── Dependencies ──────────────────────────────────────────────────────────
        private readonly GameRenderManager _renderManager;

        // ── Per-type buffer ───────────────────────────────────────────────────────
        private sealed class TypeBuffer : System.IDisposable
        {
            public NativeArray<UnitSyncData>   Data;
            public readonly Dictionary<int,int> InstanceToSlot; // instanceID → slot
            public int  HighWatermark; // render count = highest occupied slot + 1
            public bool Dirty;

            public TypeBuffer(int capacity)
            {
                Data           = new NativeArray<UnitSyncData>(capacity, Allocator.Persistent);
                InstanceToSlot = new Dictionary<int, int>(capacity);
            }

            /// <summary>Find a free slot (instanceID == 0). Returns -1 if full.</summary>
            public int AllocateSlot()
            {
                for (int i = 0; i < Data.Length; i++)
                {
                    if (Data[i].instanceID == 0)
                    {
                        if (i + 1 > HighWatermark) HighWatermark = i + 1;
                        return i;
                    }
                }
                return -1;
            }

            /// <summary>Zero a slot and compact the high-watermark downward.</summary>
            public void FreeSlot(int slot, int instanceID)
            {
                Data[slot] = default; // instanceID=0, scale=0 → invisible
                InstanceToSlot.Remove(instanceID);
                while (HighWatermark > 0 && Data[HighWatermark - 1].instanceID == 0)
                    HighWatermark--;
            }

            /// <summary>Zero every slot and reset all tracking.</summary>
            public void Clear()
            {
                for (int i = 0; i < HighWatermark; i++) Data[i] = default;
                InstanceToSlot.Clear();
                HighWatermark = 0;
            }

            public void Dispose()
            {
                if (Data.IsCreated) Data.Dispose();
            }
        }

        private readonly Dictionary<string, TypeBuffer> _buffers =
            new Dictionary<string, TypeBuffer>();

        // ── Constructor ───────────────────────────────────────────────────────────
        public Render2DService(GameRenderManager renderManager)
        {
            _renderManager = renderManager;
        }

        // ── IInitializable ────────────────────────────────────────────────────────
        /// <summary>Buffers are allocated lazily on first use of a unitID.</summary>
        public void Initialize() { }

        // ── ITickable — single flush per frame ────────────────────────────────────
        // Graphics.DrawMeshInstanced must be called EVERY frame or the batch disappears.
        // Dirty is kept only for future use (e.g. skipping expensive CPU-side work).
        public void Tick()
        {
            foreach (var kv in _buffers)
            {
                var buf = kv.Value;
                if (buf.HighWatermark > 0)
                {
                    _renderManager.PushDataToRender(kv.Key, buf.Data, buf.HighWatermark);
                    buf.Dirty = false;
                }
            }
        }

        // ── IDisposable ───────────────────────────────────────────────────────────
        public void Dispose()
        {
            foreach (var buf in _buffers.Values) buf.Dispose();
            _buffers.Clear();
        }

        // ── IRender2DService ──────────────────────────────────────────────────────

        /// <summary>
        /// Register and show a unit for the first time.
        /// If the instanceID is already registered, delegates to UpdateRender.
        /// </summary>
        public void RenderUnit(string unitID, int instanceID, Vector3 position,
                               float rotation = 0f, float scale = 1f)
        {
            var buf = GetOrCreateBuffer(unitID);
            if (buf == null) return;

            if (buf.InstanceToSlot.ContainsKey(instanceID))
            {
                UpdateRender(unitID, instanceID, position, rotation, scale);
                return;
            }

            int slot = buf.AllocateSlot();
            if (slot < 0)
            {
                Debug.LogWarning($"[Render2DService] Buffer full for '{unitID}' (cap={buf.Data.Length}).");
                return;
            }

            buf.InstanceToSlot[instanceID] = slot;
            buf.Data[slot] = BuildSync(instanceID, position, rotation, scale);
            buf.Dirty = true;
        }

        /// <summary>Update position/rotation/scale for an already-registered unit.</summary>
        public void UpdateRender(string unitID, int instanceID, Vector3 position,
                                 float rotation = 0f, float scale = 1f)
        {
            if (!_buffers.TryGetValue(unitID, out var buf)) return;
            if (!buf.InstanceToSlot.TryGetValue(instanceID, out int slot)) return;

            var s       = buf.Data[slot];
            s.position  = new float2(position.x, position.y);
            s.rotation  = rotation;
            s.scale     = scale;
            buf.Data[slot] = s;
            buf.Dirty      = true;
        }

        /// <summary>Remove one unit from the render pipeline.</summary>
        public void RemoveRender(string unitID, int instanceID)
        {
            if (!_buffers.TryGetValue(unitID, out var buf)) return;
            if (!buf.InstanceToSlot.TryGetValue(instanceID, out int slot)) return;

            buf.FreeSlot(slot, instanceID);
            buf.Dirty = true;
        }

        /// <summary>Clear all instances of a unit type and push an empty frame.</summary>
        public void RemoveAllOfType(string unitID)
        {
            if (!_buffers.TryGetValue(unitID, out var buf)) return;
            buf.Clear();
            _renderManager.PushDataToRender(unitID, buf.Data, 0);
            buf.Dirty = false;
        }

        /// <summary>Update animation state for a specific instance.</summary>
        public void SetAnimationState(string unitID, int instanceID,
                                      int animIndex, float playSpeed = 1f)
        {
            if (!_buffers.TryGetValue(unitID, out var buf)) return;
            if (!buf.InstanceToSlot.TryGetValue(instanceID, out int slot)) return;

            var s       = buf.Data[slot];
            s.animIndex = animIndex;
            s.playSpeed = playSpeed;
            buf.Data[slot] = s;
            buf.Dirty      = true;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private TypeBuffer GetOrCreateBuffer(string unitID)
        {
            if (_buffers.TryGetValue(unitID, out var existing)) return existing;

            var profile = _renderManager.gameDatabase?.GetUnitByID(unitID);
            if (profile == null)
            {
                Debug.LogWarning($"[Render2DService] No profile for '{unitID}' in database.");
                return null;
            }

            var buf = new TypeBuffer(profile.maxCapacity);
            _buffers[unitID] = buf;
            return buf;
        }

        private static UnitSyncData BuildSync(int instanceID, Vector3 position,
                                              float rotation, float scale)
            => new UnitSyncData
            {
                instanceID = instanceID,
                position   = new float2(position.x, position.y),
                rotation   = rotation,
                scale      = scale,
                animIndex  = 0,
                playSpeed  = 1f,
            };
    }
}
