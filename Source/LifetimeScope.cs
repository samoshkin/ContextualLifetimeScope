using System;

namespace ContextualLifetimeScope
{
	public class LifetimeScope<TContext> : IDisposable
	{
		public LifetimeScope()
		{
			LifetimeScopeStore.Get<TContext>().OpenScope();
		}

		public void Dispose()
		{
			LifetimeScopeStore.Get<TContext>().CloseScope();
		}
	}
}