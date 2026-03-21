using UnityEngine;
using UnityEngine.UI;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Attaches to the "Build Tower" UI Button.
    /// Finds TowerDragDropManager in the scene at Start and wires onClick automatically,
    /// so no manual Inspector drag-drop is needed.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BuildTowerButton : MonoBehaviour
    {
        private void Start()
        {
            var btn = GetComponent<Button>();
            var manager = FindObjectOfType<TowerDragDropManager>();

            if (manager == null)
            {
                Debug.LogError("[BuildTowerButton] TowerDragDropManager not found in scene.");
                return;
            }

            // Wire click → StartDragging at runtime; no manual Inspector setup required.
            btn.onClick.AddListener(manager.StartDragging);
            Debug.Log("[BuildTowerButton] Wired onClick → TowerDragDropManager.StartDragging()");
        }
    }
}
