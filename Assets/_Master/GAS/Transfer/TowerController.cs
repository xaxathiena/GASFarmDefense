using FD.Data;
using GAS;
using UnityEngine;
using VContainer.Unity;

namespace FD
{
    public readonly struct EventTowerDestroyed
    {
        public readonly string TowerId;
        public readonly Vector3 Position;
        public EventTowerDestroyed(string towerId, Vector3 position)
        {
            TowerId = towerId;
            Position = position;
        }
    }
    public class TowerController
    {
        // TowerController implementation
        private readonly AbilitySystemComponent acs;
        private readonly IDebugService debug;
        private readonly IEventBus eventBus;
        private readonly IPoolManager poolManager;
        private TowerData towerData;
        private TowerView towerView;
        private string id;
        private bool isShow = false;
        public string Id => id;
        public TowerController(IDebugService debug,
        AbilitySystemComponent acs,
        IEventBus eventBus,
        IPoolManager poolManager)
        {
            this.debug = debug;
            this.acs = acs;
            this.eventBus = eventBus;
            this.poolManager = poolManager;
            id = System.Guid.NewGuid().ToString();
        }

        public void OnSetup(TowerView towerView, TowerData towerData)
        {
            this.towerData = towerData;
            this.towerView = towerView;
        }

        public void Tick()
        {
            if (!isShow)
            {
                isShow = true;
                debug.Log($"TowerController {id} setup with TowerView and TowerData: {this.acs.Id}, {towerData}", Color.magenta);
                acs.Tick();
            }
        }
        public void Destroy()
        {
            debug.Log($"TowerController {id} is being destroyed!", Color.red);
            poolManager.Despawn(towerView); // Pass the actual TowerView instance if available˝
            eventBus.Publish(new EventTowerDestroyed(id, Vector3.zero)); // You can set the actual position if needed
        }
    }
}