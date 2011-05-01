using System;

namespace ContextualLifetimeScope.Tests
{
	public interface ISimpleService : IDisposable
	{ }

	public interface ICompositeService : IDisposable
	{
		ISimpleService SimpleService { get; }
	}

	public class SimpleServiceImpl : ISimpleService
	{
		public int TimesDisposed { get; set; }

		public void Dispose()
		{
			TimesDisposed++;
		}
	}

	public class CompositeServiceImpl : ICompositeService
	{
		public CompositeServiceImpl(ISimpleService simpleService)
		{
			SimpleService = simpleService;
		}

		public ISimpleService SimpleService { get; set; }

		public void Dispose()
		{ }
	}
}