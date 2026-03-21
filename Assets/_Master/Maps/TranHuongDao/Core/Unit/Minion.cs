using System;
using UnityEngine;
using GAS;
using Abel.TranHuongDao.Core.VFX;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// A generic unit entity that uses a modular Logic Brain.
    /// Can represent Summons, Minions, or even basic Enemies/Towers.
    /// </summary>
    public class Minion : IGASAvatar
    {
        public int InstanceID { get; private set; }
        public string UnitID { get; private set; }
        
        // --- IGASAvatar Implementation ---
        public Vector3 Position { get; set; }
        public Vector3 Scale => Vector3.one;
        public bool IsValid => true;
        Quaternion IGASAvatar.Rotation => Quaternion.Euler(0, Rotation, 0);

        public float Rotation { get; set; }

        public AbilitySystemComponent ASC { get; }
        public UnitAttributeSet AttributeSet { get; } = new UnitAttributeSet();
        public UnitConfig Config { get; private set; }
        
        // The resolved ability definition from config
        public GameplayAbilityData AttackAbility { get; private set; }

        private readonly IUnitLogic _logic;
        private IRender2DService _renderService;
        private StatusEffectVFXController _vfxController;
        private bool _renderInitialized;

        public event Action<Minion> OnDestroyed;

        public Minion(AbilitySystemComponent asc, IUnitLogic logic)
        {
            ASC = asc;
            _logic = logic;
        }

        public void Initialize(
            int instanceID,
            string unitID,
            UnitConfig config,
            GameplayAbilityData attackAbility,
            Vector3 position,
            IRender2DService renderService,
            FD.IEventBus eventBus,
            FD.Modules.VFX.IVFXManager vfxManager,
            TagVFXConfig vfxConfig)
        {
            InstanceID = instanceID;
            UnitID = unitID;
            Config = config;
            AttackAbility = attackAbility;
            Position = position;
            _renderService = renderService;

            // GAS setup
            ASC.UnitInstanceID = instanceID;
            AttributeSet.InitializeFromConfig(config);
            ASC.InitializeAttributeSet(AttributeSet);
            ASC.InitAvatar(this);

            // Grant attack ability
            if (AttackAbility != null)
            {
                ASC.GiveAbility(AttackAbility);
            }
            else if (!string.IsNullOrEmpty(Config.AttackAbilityID))
            {
                Debug.LogWarning($"[Minion] AttackAbilityID '{Config.AttackAbilityID}' was provided but could not be resolved to a GameplayAbilityData asset for unit {UnitID}.");
            }

            // Render & VFX
            renderService.RenderUnit(UnitID, InstanceID, Position, Rotation);
            _renderInitialized = true;
            
            _vfxController = new StatusEffectVFXController(instanceID, () => Position, eventBus, vfxManager, vfxConfig);

            _logic?.OnEnter(this);
        }

        public void Tick(float dt)
        {
            ASC.Tick();
            _logic?.Tick(this, dt);

            if (_renderInitialized)
                _renderService.UpdateRender(UnitID, InstanceID, Position, Rotation);
            
            _vfxController?.Tick(dt);
        }

        public void Destroy()
        {
            _logic?.OnExit(this);
            OnDestroyed?.Invoke(this);
            Cleanup();
        }

        private void Cleanup()
        {
            if (_renderInitialized)
            {
                _renderService.RemoveRender(UnitID, InstanceID);
                _renderInitialized = false;
            }
            _vfxController?.Dispose();
        }
    }
}
