using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using GAS;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Manages spawning, ticking, and cleanup of all summoned units (Minions).
    /// </summary>
    public class MinionManager : ITickable, IDisposable
    {
        private readonly IObjectResolver _container;
        private readonly IInstanceIDService _idService;
        private readonly IConfigService _configService;
        private readonly IRender2DService _renderService;
        private readonly FD.IEventBus _eventBus;
        private readonly FD.Modules.VFX.IVFXManager _vfxManager;
        private readonly UnitLogicFactory _logicFactory;

        private readonly Dictionary<int, Minion> _activeMinions = new Dictionary<int, Minion>();
        private readonly List<Minion> _pendingRemoval = new List<Minion>();

        public MinionManager(
            IObjectResolver container,
            IInstanceIDService idService,
            IConfigService configService,
            IRender2DService renderService,
            FD.IEventBus eventBus,
            FD.Modules.VFX.IVFXManager vfxManager,
            UnitLogicFactory logicFactory)
        {
            _container = container;
            _idService = idService;
            _configService = configService;
            _renderService = renderService;
            _eventBus = eventBus;
            _vfxManager = vfxManager;
            _logicFactory = logicFactory;
        }

        public Minion SpawnMinion(string unitID, Vector3 position, EUnitLogicType overrideLogic = EUnitLogicType.None)
        {
            var unitsConfig = _configService.GetConfig<UnitsConfig>();
            if (!unitsConfig.TryGetConfig(unitID, out var config))
            {
                Debug.LogWarning($"[MinionManager] UnitConfig '{unitID}' not found.");
                return null;
            }

            // Resolve Logic: Use override if specified, otherwise fallback to config default
            EUnitLogicType logicToUse = overrideLogic;
            IUnitLogic logic = _logicFactory.CreateLogic(logicToUse);

            // Resolve ASC and create Minion
            var asc = _container.Resolve<AbilitySystemComponent>();
            var minion = new Minion(asc, logic);
            int instanceID = _idService.GetNextID();

            // Resolve Attack Ability
            GameplayAbilityData attackAbility = null;
            if (!string.IsNullOrEmpty(config.AttackAbilityID))
            {
                var abilitiesConfig = _configService.GetConfig<AbilitiesConfig>();
                abilitiesConfig.TryGetAbility(config.AttackAbilityID, out attackAbility);
            }

            var tagVfxConfig = _configService.GetConfig<TagVFXConfig>();

            minion.Initialize(instanceID, unitID, config, attackAbility, position, _renderService, _eventBus, _vfxManager, tagVfxConfig);
            minion.OnDestroyed += HandleMinionDestroyed;

            _activeMinions.Add(instanceID, minion);
            return minion;
        }

        public void Tick()
        {
            float dt = Time.deltaTime;

            foreach (var minion in _activeMinions.Values)
            {
                minion.Tick(dt);
            }

            FlushPendingRemovals();
        }

        private void FlushPendingRemovals()
        {
            if (_pendingRemoval.Count == 0) return;

            foreach (var minion in _pendingRemoval)
            {
                if (_activeMinions.ContainsKey(minion.InstanceID))
                {
                    _activeMinions.Remove(minion.InstanceID);
                    minion.OnDestroyed -= HandleMinionDestroyed;
                }
            }

            _pendingRemoval.Clear();
        }

        private void HandleMinionDestroyed(Minion minion)
        {
            if (!_pendingRemoval.Contains(minion))
                _pendingRemoval.Add(minion);
        }

        public void Dispose()
        {
            var survivors = new List<Minion>(_activeMinions.Values);
            foreach (var minion in survivors)
            {
                minion.Destroy();
            }
            FlushPendingRemovals();
            _activeMinions.Clear();
        }
    }
}
