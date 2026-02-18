using UnityEngine;
using VContainer;
using VContainer.Unity;

public class BulletGameLifetimeScope : LifetimeScope
{
    [SerializeField] private BulletSystem bulletSystem;
    [SerializeField] private GunController gunController;
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(bulletSystem)
               .As<BulletSystem>();
        builder.RegisterComponent(gunController).As<GunController>();
    }
}
