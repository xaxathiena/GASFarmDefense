using UnityEngine;
using VContainer;
using Abel.TowerDefense.Render;
using Abel.TowerDefense.Core;

namespace Abel.TowerDefense.DebugTools
{
    public class UnitDebugger : MonoBehaviour
    {
        private GameUnitManager unitManager;
        [Inject] 
        public void Construct(GameUnitManager manager)
        {
            unitManager = manager;
        }
        [Header("Settings")]
        public float clickRadius = 1.5f; // Mouse click detection radius
        public Color selectionColor = Color.green;

        // Selection State
        private int selectedIndex = -1;
        private UnitGroupBase selectedGroup = null; // Which group does the unit belong to?
        private string groupName = "";
        
        void Update()
        {
            // Detect Left Click
            if (Input.GetMouseButtonDown(0))
            {
                SelectUnitUnderMouse();
            }
        }

        private void SelectUnitUnderMouse()
        {
            // 1. Raycast to Ground Plane (Mathematical Plane, no Colliders needed)
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // XZ Plane at Y=0

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint3D = ray.GetPoint(enter);
                Vector2 hitPoint2D = new Vector2(hitPoint3D.x, hitPoint3D.z);

                // 2. Check Group A
                // We need to expose groups from GameManager or add a public getter.
                // Assuming GameManager exposes GetGroupA() and GetGroupB() 
                // OR we just make them public for Debugging.
                
                // For this example, let's assume GameUnitManager has public fields or getters
                var loadedGroups = unitManager.LoadedGroups;
                foreach (var group in loadedGroups.Values)
                {
                    int idx = group.FindUnitIndexAt(hitPoint2D, clickRadius);
                    if (idx != -1)
                    {
                        SetSelection(group, idx, group.GroupName);
                        return;
                    }
                }
                // Clicked empty space
                Deselect();
            }
        }

        private void SetSelection(UnitGroupBase group, int index, string name)
        {
            selectedGroup = group;
            selectedIndex = index;
            groupName = name;
            Debug.Log($"Selected {name} ID: {index}");
        }

        private void Deselect()
        {
            selectedGroup = null;
            selectedIndex = -1;
        }

        // --- VISUALIZATION (ON GUI) ---
        void OnGUI()
        {
            if (selectedGroup == null || selectedIndex == -1) return;

            // Ensure unit is still alive (Index valid)
            if (selectedIndex >= selectedGroup.ActiveCount)
            {
                Deselect();
                return;
            }

            // Get Data (Copy struct)
            var render = selectedGroup.RenderData[selectedIndex];
            var logic = selectedGroup.LogicData[selectedIndex];

            // Draw Box
            float width = 250;
            float height = 180;
            float x = 20;
            float y = Screen.height - height - 20;
            
            GUI.Box(new Rect(x, y, width, height), $"DEBUG: {groupName} [{selectedIndex}]");

            GUILayout.BeginArea(new Rect(x + 10, y + 25, width - 20, height - 30));
            
            GUILayout.Label($"<b>--- Render Data ---</b>");
            GUILayout.Label($"Pos: {render.position}");
            GUILayout.Label($"Rot: {render.rotation:F1}°");
            GUILayout.Label($"Anim: Idx {render.animIndex} | Time: {render.animTimer:F2}");

            GUILayout.Space(5);
            GUILayout.Label($"<b>--- Logic Data ---</b>");
            GUILayout.Label($"State: <color=yellow>{logic.currentState}</color>");
            GUILayout.Label($"State Timer: {logic.stateTimer:F2}s");
            GUILayout.Label($"Atk Speed: {logic.attackSpeed}");

            GUILayout.EndArea();
        }

        // --- DRAW CIRCLE IN SCENE ---
        void OnDrawGizmos()
        {
            if (selectedGroup != null && selectedIndex != -1 && selectedIndex < selectedGroup.ActiveCount)
            {
                var data = selectedGroup.RenderData[selectedIndex];
                Vector3 pos = new Vector3(data.position.x, 0, data.position.y);

                Gizmos.color = selectionColor;
                Gizmos.DrawWireSphere(pos, 1.0f);
                Gizmos.DrawLine(pos, pos + Vector3.up * 2);
            }
        }
    }
}