using UnityEngine;
using Unity.Collections;
using Abel.TowerDefense.Data; // UnitRenderData

namespace Abel.TowerDefense.Render
{
    /// <summary>
    /// Batches health-bar quads for all visible units and issues a single
    /// Graphics.RenderMeshInstanced call per frame.
    /// Each bar reads hpPercent from UnitRenderData and forwards it to the
    /// GPU via the MaterialPropertyBlock property "_HPPercent".
    /// </summary>
    public class HealthBarBatchRenderer : System.IDisposable
    {
        // ── Quad geometry & material ─────────────────────────────────────────
        private readonly Mesh     quadMesh;
        private readonly Material hpMaterial;

        // ── Per-instance GPU data ─────────────────────────────────────────────
        private readonly Matrix4x4[] matrices;    // TRS transform for every bar
        private readonly float[]     hpPercents;  // Normalized HP [0.0 .. 1.0] per bar

        // ── Render configuration ──────────────────────────────────────────────
        private RenderParams renderParams;

        // Visual constants
        // Y offset: lifts the bar above the sprite in world-Y so it appears
        // "on top" when viewed through the 45-degree orthographic camera.
        private static readonly Vector3 BarOffset    = new Vector3(0f, 1.2f, 0f);

        // Thin rectangular quad: wide enough to read, thin enough to be subtle.
        private static readonly Vector3 BarScale     = new Vector3(0.8f, 0.15f, 1f);

        // Match the camera tilt so the bar lies in the same billboard plane as the unit sprite.
        private static readonly Quaternion BarRotation = Quaternion.Euler(45f, 0f, 0f);

        // Pre-baked TRS rotation/scale portion (constant across all instances).
        private static readonly Matrix4x4  BarRS;

        // ── Static constructor ────────────────────────────────────────────────
        static HealthBarBatchRenderer()
        {
            // Cache the rotation+scale matrix once; only translation changes per instance.
            BarRS = Matrix4x4.TRS(Vector3.zero, BarRotation, BarScale);
        }

        // ── Constructor ───────────────────────────────────────────────────────
        /// <summary>
        /// Allocates all per-instance arrays and sets up the RenderParams.
        /// </summary>
        /// <param name="quadMesh">A simple quad mesh (two triangles) for the bar.</param>
        /// <param name="hpMaterial">Material that reads "_HPPercent" and draws the bar.</param>
        /// <param name="capacity">Maximum number of bars rendered in a single frame.</param>
        public HealthBarBatchRenderer(Mesh quadMesh, Material hpMaterial, int capacity)
        {
            this.quadMesh   = quadMesh;
            this.hpMaterial = hpMaterial;

            // Pre-allocate managed arrays to avoid per-frame GC pressure.
            matrices   = new Matrix4x4[capacity];
            hpPercents = new float[capacity];

            // Configure RenderParams once; only matProps data is updated each frame.
            renderParams = new RenderParams(hpMaterial)
            {
                matProps = new MaterialPropertyBlock(),
                // Large bounds prevent the draw call from being frustum-culled globally.
                worldBounds = new Bounds(Vector3.zero, Vector3.one * 10000f)
            };
        }

        // ── Public API ────────────────────────────────────────────────────────
        /// <summary>
        /// Builds per-instance matrices and HP values, then dispatches a single
        /// GPU instanced draw call for all health bars.
        /// </summary>
        /// <param name="units">Flat NativeArray of render data produced by the logic system.</param>
        /// <param name="count">Number of valid entries to read from <paramref name="units"/>.</param>
        public void Render(NativeArray<UnitRenderData> units, int count)
        {
            for (int i = 0; i < count; i++)
            {
                UnitRenderData u = units[i];

                // Map the logical 2D position onto the 3D XZ plane, then lift in world-Y.
                // This matches how UnitBatchRenderer positions sprites.
                Vector3 worldPos = new Vector3(u.position.x, 0f, u.position.y) + BarOffset;

                // Build the TRS matrix by composing the constant RS block with the world translation.
                // Equivalent to Matrix4x4.TRS(worldPos, BarRotation, BarScale) but avoids
                // recomputing the rotation matrix every iteration.
                matrices[i] = Matrix4x4.Translate(worldPos) * BarRS;

                // Clamp to [0,1] as a defensive measure in case the logic system overshoots.
                hpPercents[i] = Mathf.Clamp01(u.hpPercent);
            }

            // Upload HP data to the GPU via MaterialPropertyBlock.
            // The shader must declare: UNITY_INSTANCING_BUFFER_START / float _HPPercent
            renderParams.matProps.SetFloatArray("_HPPercent", hpPercents);

            // Single draw call renders all bars as instanced geometry.
            Graphics.RenderMeshInstanced(renderParams, quadMesh, 0, matrices, count);
        }

        // ── IDisposable ───────────────────────────────────────────────────────
        /// <summary>
        /// Releases the cloned material to prevent memory leaks.
        /// Call this when the owning system is torn down (e.g., OnDestroy / Dispose).
        /// </summary>
        public void Dispose()
        {
            // Nothing native to free here; arrays are managed.
            // If hpMaterial was cloned (new Material(...)), destroy it to avoid leaks.
            if (hpMaterial != null)
            {
#if UNITY_EDITOR
                Object.DestroyImmediate(hpMaterial);
#else
                Object.Destroy(hpMaterial);
#endif
            }
        }
    }
}
