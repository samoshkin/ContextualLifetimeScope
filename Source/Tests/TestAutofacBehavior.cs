using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Core.Registration;
using Autofac.Features.OwnedInstances;
using NUnit.Framework;

namespace ContextualLifetimeScope.Tests
{
	[TestFixture]
	public class TestAutofacBehavior
	{
		[Test]
		public void TransientInstances()
		{
			var containerBuilder = new ContainerBuilder();
			containerBuilder.RegisterType<SimpleServiceImpl>().As<ISimpleService>().InstancePerDependency();

			using(var container = containerBuilder.Build())
			{
				var simpleService1 = container.Resolve<ISimpleService>();
				var simpleService2 = container.Resolve<ISimpleService>();
			}

			Console.WriteLine();	
		}

		[Test]
		public void Nested_litetime_scopes()
		{
			var containerBuilder = new ContainerBuilder();
			containerBuilder.RegisterType<SimpleServiceImpl>().As<ISimpleService>().InstancePerDependency();
			containerBuilder.RegisterType<CompositeServiceImpl>().As<ICompositeService>().InstancePerLifetimeScope();

			var container = containerBuilder.Build();

			var simpleService1 = container.Resolve<ISimpleService>();
			using(var lifetime = container.BeginLifetimeScope())
			{
				var simpleService2 = lifetime.Resolve<ISimpleService>();
				var compositeService1 = lifetime.Resolve<ICompositeService>();
				var compositeService2 = lifetime.Resolve<ICompositeService>();

				Assert.AreNotEqual(compositeService1.SimpleService, simpleService2);
				Assert.AreNotEqual(compositeService1.SimpleService, simpleService1);

				Assert.AreEqual(compositeService1, compositeService2);
				Assert.AreEqual(compositeService1.SimpleService, compositeService2.SimpleService);

				using(var lifetime1 = container.BeginLifetimeScope())
				{
					var compositeService3 = lifetime1.Resolve<ICompositeService>();
					Assert.AreNotEqual(compositeService3, compositeService2);
				}
			}
		}

		[Test]
		public void Tagged_nested_litetime_scopes()
		{
			var containerBuilder = new ContainerBuilder();
			containerBuilder.RegisterType<SimpleServiceImpl>().As<ISimpleService>().InstancePerMatchingLifetimeScope(1);
			containerBuilder.RegisterType<CompositeServiceImpl>().As<ICompositeService>().InstancePerMatchingLifetimeScope(2);

			var container = containerBuilder.Build();

			Assert.Throws<DependencyResolutionException >(()=>container.Resolve<ISimpleService>());
			using (var lifetime = container.BeginLifetimeScope(1))
			{
				var simpleService1 = lifetime.Resolve<ISimpleService>();
				using (var lifetime1 = lifetime.BeginLifetimeScope(2))
				{
					var compositeService1 = lifetime1.Resolve<ICompositeService>();
					Assert.AreEqual(compositeService1.SimpleService, simpleService1);
				}
			}
		}

		[Test]
		public void Owned_instances()
		{
			var containerBuilder = new ContainerBuilder();
			containerBuilder.RegisterType<SimpleServiceImpl>().As<ISimpleService>()
				.InstancePerLifetimeScope();
			containerBuilder.RegisterType<CompositeServiceImpl>().As<ICompositeService>().InstancePerDependency();
			containerBuilder.RegisterType<SimpleContainerService>().As<ISimpleContainerService>().InstancePerDependency();
			containerBuilder.RegisterType<Root>().InstancePerDependency();
			containerBuilder.Register(c => new ServiceLocator(c.Resolve<IComponentContext>())).SingleInstance();
			//containerBuilder.RegisterSource(new ResolveServiceLocatorSource());

			var container = containerBuilder.Build();

			//var ss = container.Resolve<ServiceLocator>().ResolveService<ISimpleService>();

			var root = container.Resolve<Root>();
			var compService = root.DoSomething();

			
			//Assert.AreEqual(root.ServiceCreateByLocator, ((CompositeServiceImpl) compService).SimpleService);
			Assert.AreEqual(compService.SimpleService, ((CompositeServiceImpl)compService).ServiceCreateByLocator);
			
			/*Assert.AreEqual(root.ServiceCreateByLocator, ss);
			Assert.AreEqual(((CompositeServiceImpl)compService).ServiceCreateByLocator, ss);*/
			//Assert.AreEqual(root.ServiceCreateByLocator, ss);
		}
	}


	public class ResolveServiceLocatorSource : IRegistrationSource
	{
		public IEnumerable<IComponentRegistration> RegistrationsFor(
			Service service,
			Func<Service, IEnumerable<IComponentRegistration>> registrationAccessor)
		{
			var ts = service as TypedService;
			if (ts != null && ts.ServiceType==typeof(ServiceLocator))
			{
				return new[]
				{
					RegistrationBuilder
						.ForDelegate((c, p) => new ServiceLocator(c.Resolve<IComponentContext>()))
						.CreateRegistration()
				};
			}

			return Enumerable.Empty<IComponentRegistration>();
		}

		public bool IsAdapterForIndividualComponents
		{
			get { return true; }
		}
	}
	

	public class ServiceLocator
	{
		private readonly IComponentContext _context;

		public ServiceLocator(IComponentContext context)
		{
			_context = context;
		}

		public T ResolveService<T>()
		{
			return _context.Resolve<T>();
		}
	}

	public class Root
	{
		private readonly Owned<ICompositeService> _compService;
		private ServiceLocator _locator;

		public Root(
			Owned<ICompositeService> compService,
			ServiceLocator locator)
		{
			_compService = compService;
			_locator = locator;
		}

		public ISimpleService SimpleService { get; set; }
		public ISimpleService ServiceCreateByLocator { get; set; }

		public ICompositeService DoSomething()
		{
			using(_compService)
			{
				ServiceCreateByLocator = _locator.ResolveService<ISimpleService>();
				return _compService.Value;
			}
		}

	}

	public interface ISimpleContainerService
	{
		ISimpleService Service { get; }
	}

	class SimpleContainerService : ISimpleContainerService
	{
		public SimpleContainerService(ISimpleService service)
		{
			Service = service;
		}

		public ISimpleService Service { get; set; }
	}

	public interface ICompositeService : IDisposable
	{
		ISimpleService SimpleService { get; }
		ISimpleContainerService Container { get; }
	}

	public class CompositeServiceImpl : ICompositeService
	{
		public CompositeServiceImpl(
			ServiceLocator locator,
			ISimpleService simpleService,
			ISimpleContainerService container)
		{
			SimpleService = simpleService;
			Container = container;
			ServiceCreateByLocator = locator.ResolveService<ISimpleService>();
		}

		public ISimpleService ServiceCreateByLocator { get; set; }

		public ISimpleService SimpleService { get; set; }

		public ISimpleContainerService Container { get; set; }

		public void Dispose()
		{ }
	}
}
