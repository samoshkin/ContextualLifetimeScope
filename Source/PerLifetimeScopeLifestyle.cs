using System;
using Castle.MicroKernel.Context;
using Castle.MicroKernel.Lifestyle;

namespace ContextualLifetimeScope
{
	public class PerLifetimeScopeLifestyle<TContext> : AbstractLifestyleManager
	{
		public class ComponentInstance : IDisposable
		{
			public ComponentInstance(PerLifetimeScopeLifestyle<TContext> lifestyleManager, object instance)
			{
				LifestyleManager = lifestyleManager;
				Instance = instance;
			}

			public PerLifetimeScopeLifestyle<TContext> LifestyleManager { get; private set; }
			public object Instance { get; private set; }


			public void Dispose()
			{
				LifestyleManager.ReleaseOnScopeExiting(Instance);
			}
		}
		
		public override void Dispose()
		{ }

		public override bool Release(object instance)
		{
			LifetimeScopeStore.Get<TContext>().TryRemove(this);
			return base.Release(instance);
		} 

		private bool ReleaseOnScopeExiting(object instance)
		{
			return base.Release(instance);
		}

		public override object Resolve(CreationContext context)
		{
			return LifetimeScopeStore.Get<TContext>()
				.GetOrAdd(this, lifestyleManager => new ComponentInstance(lifestyleManager, base.Resolve(context)))
				.Instance;
		}
	}

}