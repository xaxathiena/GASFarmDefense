using UnityEngine;
using VContainer.Unity;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Handles the drag-and-drop tower placement preview.
    /// Registered as a scene MonoBehaviour via VContainer's RegisterComponent,
    /// which wires ITickable into the PlayerLoop automatically.
    /// </summary>
    public class TowerDragDropManager : MonoBehaviour, ITickable
    {
        // ── Inspector references ──────────────────────────────────────────────

        [Header("Preview")]
        [Tooltip("The SpriteRenderer used as a ghost/preview icon while dragging.")]
        [SerializeField] private SpriteRenderer previewSprite;

        // ── Preview colors ────────────────────────────────────────────────────

        private static readonly Color ColorValid   = new Color(0f,  1f, 0f, 0.6f); // semi-transparent green
        private static readonly Color ColorInvalid = new Color(1f,  0f, 0f, 0.6f); // semi-transparent red

        // ── Injected dependencies ─────────────────────────────────────────────

        private IMapLayoutManager _map;
        private TowerBuilderConfig _config;
        private ITowerSpawner      _spawner;

        // ── State machine ─────────────────────────────────────────────────────

        /// <summary>True while the player is holding a tower card and dragging it.</summary>
        private bool _isDragging;

        // Raycast plane — set in Construct() once the map origin Z is known.
        // The grid uses the XY plane (GridToWorldPosition outputs X=col, Y=row, Z=const),
        // so we must intersect the Z=originZ plane, NOT the horizontal Y=0 plane.
        private Plane _groundPlane;

        // ── VContainer injection ──────────────────────────────────────────────

        /// <summary>
        /// Called once by VContainer after the container is built.
        /// Preferred over Awake/Start for injected MonoBehaviours.
        /// </summary>
        [VContainer.Inject]
        public void Construct(IMapLayoutManager map, TowerBuilderConfig config, ITowerSpawner spawner)
        {
            _map     = map;
            _config  = config;
            _spawner = spawner;

            // Build the raycast plane that matches the grid's coordinate system.
            // GridToWorldPosition(x, y) returns Vector3(X, Y, Z=originZ), so the
            // playing field lies on the XY plane at a fixed Z depth.
            // Vector3.forward (0,0,1) as normal defines the Z = const plane;
            // using cell (0,0) as a point on the plane captures the exact Z origin.
            Vector3 gridOriginSample = _map.GridToWorldPosition(0, 0);
            _groundPlane = new Plane(Vector3.forward, gridOriginSample);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // If the designer didn't wire previewSprite in the Inspector, try to
            // find it automatically on a child named "TowerPreview".
            if (previewSprite == null)
            {
                var child = transform.Find("TowerPreview");
                if (child != null)
                    previewSprite = child.GetComponent<SpriteRenderer>();
            }

            // If still null, synthesize a minimal preview object at runtime.
            if (previewSprite == null)
            {
                var go = new GameObject("TowerPreview");
                go.transform.SetParent(transform, false);
                previewSprite = go.AddComponent<SpriteRenderer>();
                previewSprite.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
                previewSprite.sortingOrder = 10;
            }

            // Hide preview icon until a drag session begins.
            SetPreviewVisible(false);
        }

        // ── ITickable (VContainer PlayerLoop) ─────────────────────────────────

        /// <summary>
        /// Runs once per frame inside VContainer's PlayerLoop — equivalent to Update().
        /// Moves and recolors the preview sprite while a drag is in progress.
        /// </summary>
        public void Tick()
        {
            // Allow pressing B as a shortcut to start dragging (for testing without UI).
            if (!_isDragging && Input.GetKeyDown(KeyCode.B))
            {
                StartDragging();
                return;
            }

            if (!_isDragging) return;

            // --- Cancel drag on right-click or Escape ----------------------------
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelDragging();
                return;
            }

            // --- Raycast mouse ray against the mathematical Y=0 plane ------------
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Plane.Raycast returns the distance along the ray to the intersection.
            if (!_groundPlane.Raycast(ray, out float distance)) return;

            Vector3 worldPos = ray.GetPoint(distance);

            // --- Snap to grid centre ---------------------------------------------
            // Convert to grid indices, then back to world to get the cell centre.
            Vector2Int gridPos    = _map.WorldToGridPosition(worldPos);
            Vector3    snappedPos = _map.GridToWorldPosition(gridPos.x, gridPos.y);

            // Keep the preview on the same layer/height as the grid.
            previewSprite.transform.position = snappedPos;

            // --- Recolor based on build legality ---------------------------------
            previewSprite.color = _map.CanBuildAt(snappedPos) ? ColorValid : ColorInvalid;

            // --- Drop on mouse button release ------------------------------------
            // Using GetMouseButtonUp instead of GetMouseButtonDown to match the
            // natural "drag then release" feel of a card-based UI.
            if (Input.GetMouseButtonUp(0))
            {
                TryDropTower(gridPos, snappedPos);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Begins a drag session. Call this from a UI Button's OnClick event.
        /// </summary>
        public void StartDragging()
        {
            _isDragging = true;
            SetPreviewVisible(true);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Executes the full drop sequence when the player releases the mouse button:
        /// validate → mark cell → get random ID → spawn tower → end drag.
        /// </summary>
        private void TryDropTower(Vector2Int gridPos, Vector3 snappedWorldPos)
        {
            if (_map.CanBuildAt(snappedWorldPos))
            {
                // Step 1: Mark the cell as occupied so nothing else can build here.
                _map.SetCellState(gridPos, GridCellType.TowerOccupied);

                // Step 2: Pick a random tower type from the config data.
                string randomID = _config.GetRandomTowerID();

                // Step 3: Delegate actual unit creation to the spawner (DOD layer).
                _spawner.SpawnTower(randomID, snappedWorldPos);

                Debug.Log($"[TowerDragDropManager] Placed '{randomID}' at grid {gridPos} (world {snappedWorldPos})");
            }
            else
            {
                Debug.Log($"[TowerDragDropManager] Drop rejected — cell {gridPos} is not Buildable.");
            }

            // Always end the drag session; the player must click the card again to retry.
            StopDragging();
        }

        /// <summary>Ends the drag session and hides the preview icon.</summary>
        private void StopDragging()
        {
            _isDragging = false;
            SetPreviewVisible(false);
        }

        /// <summary>Cancels a drag without attempting placement.</summary>
        private void CancelDragging()
        {
            Debug.Log("[TowerDragDropManager] Drag cancelled.");
            StopDragging();
        }

        /// <summary>Shows or hides the preview SpriteRenderer.</summary>
        private void SetPreviewVisible(bool visible)
        {
            if (previewSprite != null)
                previewSprite.gameObject.SetActive(visible);
        }
    }
}
