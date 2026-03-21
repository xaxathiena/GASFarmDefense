using VContainer;

namespace Abel.TranHuongDao.Core
{
    /// <summary>
    /// Factory for resolving IUnitLogic instances based on EUnitLogicType.
    /// Uses VContainer to resolve logic behaviors as singletons or transients.
    /// </summary>
    public class UnitLogicFactory
    {
        private readonly IObjectResolver _container;

        public UnitLogicFactory(IObjectResolver container)
        {
            _container = container;
        }

        public IUnitLogic CreateLogic(EUnitLogicType type)
        {
            switch (type)
            {
                case EUnitLogicType.StationaryAttack:
                    // Note: We'll implement these concrete classes next.
                    return _container.Resolve<StationaryAttackLogic>();
                case EUnitLogicType.PathFollower:
                    return _container.Resolve<PathFollowerLogic>();
                case EUnitLogicType.HomingSuicide:
                    return _container.Resolve<HomingSuicideLogic>();
                case EUnitLogicType.PetFollower:
                     return _container.Resolve<PetFollowerLogic>();
                default:
                    return null;
            }
        }
    }
}
