using UnityEngine;
using VContainer;
using Abel.TowerDefense.Render;
using System;

namespace Abel.TowerDefense.InputSystem
{
    public class InputCreateUnit : MonoBehaviour
    {
        // Inject the manager
        [SerializeField] private string unitIDToSpawn = "Goblin_Archer"; // Set this in Inspector to choose which unit to spawn
        private GameUnitManager unitManager;
        [Inject]
        public void Construct(GameUnitManager manager)
        {
            unitManager = manager;
        }
        void Update()
        {
            // Spawning logic via Input
            if (Input.GetKeyDown(KeyCode.A))
            {
                unitManager.SpawnUnit(unitIDToSpawn, GetMouseWorldPosition());
            }
        }

        private Vector2 GetMouseWorldPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // XZ Plane at Y=0

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                return new Vector2(hitPoint.x, hitPoint.z);
            }
            return Vector2.zero; // Fallback
        }
    }
}