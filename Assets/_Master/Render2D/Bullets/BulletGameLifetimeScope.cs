using UnityEngine;
using VContainer;
using VContainer.Unity;

public class BulletGameLifetimeScope : LifetimeScope
{
    [SerializeField] private BulletSystem bulletSystemPrefab;
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInNewPrefab(bulletSystemPrefab, Lifetime.Singleton)
               .As<BulletSystem>();
    }
}
