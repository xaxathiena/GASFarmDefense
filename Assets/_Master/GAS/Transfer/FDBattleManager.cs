using System.Diagnostics;
using UnityEngine;
using VContainer.Unity;

namespace FD
{
    public class FDBattleManager : IStartable, ITickable
    {
        private readonly FDBattleSceneSetting fDBattleScene;
        private readonly IPoolManager poolManager;
        private readonly IDebugService debug;
        public FDBattleManager(FDBattleSceneSetting fDBattleScene, IPoolManager poolManager, IDebugService debug)
        {
            // Constructor logic here
            this.fDBattleScene = fDBattleScene;
            this.poolManager = poolManager;
            this.debug = debug;
        }
        public void Start()
        {
            poolManager.Spawn<TowerView>(fDBattleScene.TowerPrefab, fDBattleScene.TowerSpawnPoint.position, Quaternion.identity);
            debug.Log("FDBattleManager started and tower spawned!", Color.green);
        }

        public void Tick()
        {

        }
    }
}