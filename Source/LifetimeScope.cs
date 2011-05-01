using System;

namespace ContextualLifetimeScope
{
public class LifetimeScope<TContext> : IDisposable
{
    private static readonly LifetimeScopeStore _store = new LifetimeScopeStore(typeof(TContext));

    public LifetimeScope()
    {
        Store.OpenScope();
    }

    public static LifetimeScopeStore Store
    {
        get { return _store; }
    }

    public void Dispose()
    {
        Store.CloseScope();
    }
}
}