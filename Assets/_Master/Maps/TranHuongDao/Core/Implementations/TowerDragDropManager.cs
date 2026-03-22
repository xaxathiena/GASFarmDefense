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

        private static readonly Color ColorValid = new Color(0f, 1f, 0f, 0.6f); // semi-transparent green
        private static readonly Color ColorInvalid = new Color(1f, 0f, 0f, 0.6f); // semi-transparent red

        // ── Injected dependencies ─────────────────────────────────────────────

        private IMapLayoutManager _map;
        private TowerBuilderConfig _config;
        private ITowerSpawner _spawner;
        private IConfigService _configService;

        // ── State machine ─────────────────────────────────────────────────────

        /// <summary>True while the player is holding a tower card and dragging it.</summary>
        private bool _isDragging;

        // Raycast plane — set in Construct() once the map origin Z is known.
        // The grid uses the XY plane (GridToWorldPosition outputs X=col, Y=row, Z=const),
        // so we must intersect the Z=originZ plane, NOT the horizontal Y=0 plane.
        private Plane _groundPlane;
        private UnitsConfig _unitsConfig;

        // ── VContainer injection ──────────────────────────────────────────────

        /// <summary>
        /// Called once by VContainer after the container is built.
        /// Preferred over Awake/Start for injected MonoBehaviours.
        /// </summary>
        [VContainer.Inject]
        public void Construct(IMapLayoutManager map, TowerBuilderConfig config, ITowerSpawner spawner, IConfigService iConfigService)
        {
            _map = map;
            _config = config;
            _spawner = spawner;
            _configService = iConfigService;
            _unitsConfig = _configService.GetConfig<UnitsConfig>();
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
            if (!_isDragging) return;

            // --- Cancel drag on right-click or Escape ----------------------------
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelDragging();
                return;
            }

            // --- Raycast mouse ray against plane --------------------------------
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!_groundPlane.Raycast(ray, out float distance)) return;

            Vector3 worldPos = ray.GetPoint(distance);
            Vector2Int gridPos = _map.WorldToGridPosition(worldPos);
            Vector3 snappedPos = _map.GridToWorldPosition(gridPos.x, gridPos.y);

            // Keep the preview on the same layer/height as the grid.
            previewSprite.transform.position = snappedPos;

            // --- Recolor based on build legality ---------------------------------
            bool canBuild = _map.CanBuildAt(snappedPos);
            previewSprite.color = canBuild ? ColorValid : ColorInvalid;

            // --- Drop on mouse button release (handled by Click from Card usually, but keeping here for fallback) ---
        }

        public bool IsValidPlacement(Vector2 screenPos, out Vector2Int gridPos, out Vector3 snappedPos)
        {
            gridPos = Vector2Int.zero;
            snappedPos = Vector3.zero;

            // UI Toolkit uses top-left origin, but Camera.ScreenPointToRay expects bottom-left (screen pixels).
            Vector2 correctedPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
            Ray ray = Camera.main.ScreenPointToRay(correctedPos);
            
            if (!_groundPlane.Raycast(ray, out float distance)) return false;

            Vector3 worldPos = ray.GetPoint(distance);
            gridPos = _map.WorldToGridPosition(worldPos);
            snappedPos = _map.GridToWorldPosition(gridPos.x, gridPos.y);

            return _map.CanBuildAt(snappedPos);
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

        public void TryDropTower(Vector2Int gridPos, Vector3 snappedWorldPos, string towerID = "")
        {
            if (_map.CanBuildAt(snappedWorldPos))
            {
                _map.SetCellState(gridPos, GridCellType.TowerOccupied);

                string finalID = string.IsNullOrEmpty(towerID) ? _config.GetTowerIDWithTier(1, _unitsConfig) : towerID;
                if (string.IsNullOrEmpty(finalID))
                {
                    Debug.LogError($"[TowerDragDropManager] No tower ID found!");
                    return;
                }
                _spawner.SpawnTower(finalID, snappedWorldPos);

                Debug.Log($"[TowerDragDropManager] Placed '{finalID}' at grid {gridPos} (world {snappedWorldPos})");
            }
            else
            {
                Debug.Log($"[TowerDragDropManager] Drop rejected — cell {gridPos} is not Buildable.");
            }

            // Always end the drag session; the player must click the card again to retry.
            StopDragging();
        }

        /// <summary>Ends the drag session and hides the preview icon.</summary>
        public void StopDragging()
        {
            _isDragging = false;
            SetPreviewVisible(false);
        }

        /// <summary>Cancels a drag without attempting placement.</summary>
        public void CancelDragging()
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
