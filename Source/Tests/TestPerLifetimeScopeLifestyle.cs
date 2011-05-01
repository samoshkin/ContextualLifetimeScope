using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Releasers;
using Castle.Windsor;
using NUnit.Framework;

namespace ContextualLifetimeScope.Tests
{
	[TestFixture]
	public class TestPerLifetimeScopeLifestyle
	{
		[Test]
		public void Should_resolve_dependency_in_opened_scope()
		{
			var container = new WindsorContainer();
			container.Register(Component
				.For<ISimpleService>()
				.ImplementedBy<SimpleServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<string>>());

			using (new LifetimeScope<string>())
			{
				var service = container.Resolve<ISimpleService>();
				Assert.IsInstanceOf<SimpleServiceImpl>(service);
			}
		}

		[Test]
		public void If_dependency_is_resolved_and_then_released_in_scope_should_create_another_instance_for_next_resolve_request()
		{
			var container = new WindsorContainer();
			container.Register(Component
				.For<ISimpleService>()
				.ImplementedBy<SimpleServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<string>>());

			using (new LifetimeScope<string>())
			{
				var service1 = container.Resolve<ISimpleService>();
				container.Release(service1);
				var service2 = container.Resolve<ISimpleService>();
				
				Assert.AreNotEqual(service1, service2);
			}
		}

		[Test]
		public void If_container_track_dependecies_and_dependency_is_released_in_scope_should_not_release_it_again_when_exiting_the_scope()
		{
			var container = new WindsorContainer();
			container.Register(Component
				.For<ISimpleService>()
				.ImplementedBy<SimpleServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<string>>());

			SimpleServiceImpl service1;
			using (new LifetimeScope<string>())
			{
				service1 = (SimpleServiceImpl)container.Resolve<ISimpleService>();
				container.Release(service1);
			}

			Assert.AreEqual(1, service1.TimesDisposed);
		}

		[Test]
		public void If_container_does_not_track_dependencies_manual_releasing_of_dependency_should_not_make_any_effect()
		{
			var container = new WindsorContainer();
			container.Kernel.ReleasePolicy = new NoTrackingReleasePolicy();
			container.Register(Component
				.For<ISimpleService>()
				.ImplementedBy<SimpleServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<string>>());

			using(new LifetimeScope<string>())
			{
				var service1 = (SimpleServiceImpl)container.Resolve<ISimpleService>();
				container.Release(service1);

				Assert.AreEqual(0, service1.TimesDisposed);	
			}
		}

		[Test]
		public void Even_if_container_does_not_track_dependecies_should_release_dependency_when_exiting_the_scope()
		{
			var container = new WindsorContainer();
			container.Kernel.ReleasePolicy = new NoTrackingReleasePolicy();
			container.Register(Component
				.For<ISimpleService>()
				.ImplementedBy<SimpleServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<string>>());

			SimpleServiceImpl service;
			using(var scope = new LifetimeScope<string>())
			{
				service = (SimpleServiceImpl)container.Resolve<ISimpleService>();
			}

			Assert.AreEqual(1, service.TimesDisposed);	
		}

		[Test]
		public void When_resolving_same_service_in_same_scope_several_times_should_return_the_same_component()
		{
			var container = new WindsorContainer();
			container.Register(Component
				.For<ISimpleService>()
				.ImplementedBy<SimpleServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<string>>());

			using (new LifetimeScope<string>())
			{
				var service1 = container.Resolve<ISimpleService>();
				var service2 = container.Resolve<ISimpleService>();
				Assert.AreEqual(service1, service2);
			}
		}

		[Test]
		public void When_resolving_same_service_in_different_scope_several_times_should_return_different_components()
		{
			var container = new WindsorContainer();
			container.Register(Component
				.For<ISimpleService>()
				.ImplementedBy<SimpleServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<string>>());

			ISimpleService service1, service2;
			using (new LifetimeScope<string>())
			{
				service1 = container.Resolve<ISimpleService>();
			}
			using (new LifetimeScope<string>())
			{
				service2 = container.Resolve<ISimpleService>();
			}

			Assert.AreNotEqual(service1, service2);
		}

		[Test]
		public void If_no_scope_is_opened_should_fail_to_resolve_dependency()
		{
			var container = new WindsorContainer();
			container.Register(Component
				.For<ISimpleService>()
				.ImplementedBy<SimpleServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<string>>());

			Assert.Throws<InvalidOperationException>(
				() => container.Resolve<ISimpleService>(),
				"Dependency could be resolved only when 'String' scope is opened.");
		}

		[Test]
		public void Should_fail_to_open_nested_scope_of_same_context()
		{
			using (new LifetimeScope<string>())
			{
				var fault = Assert.Throws<InvalidOperationException>(
					() => new LifetimeScope<string>(),
					"'String' scope is valid only as a root scope. Lifetime scopes do not support nested scoped of same context.");
				Console.WriteLine(fault.Message);
			}
		}

		[Test]
		public void If_service_in_scope_has_dependecy_to_service_which_is_out_of_scope_container_should_wire_it()
		{
			var container = new WindsorContainer();
			container.Register(Component
				.For<ISimpleService>()
				.ImplementedBy<SimpleServiceImpl>()
				.LifeStyle.Singleton);
			container.Register(Component
				.For<ICompositeService>()
				.ImplementedBy<CompositeServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<string>>());

			var simpleService = container.Resolve<ISimpleService>();
			using (new LifetimeScope<string>())
			{
				var compositeService = container.Resolve<ICompositeService>();
				Assert.AreEqual(simpleService, compositeService.SimpleService);
			}
		}

		[Test]
		public void If_service_in_scope_has_dependecy_to_service_which_is_in_outer_scope_container_should_wire_it()
		{
			var container = new WindsorContainer();
			container.Register(Component
				.For<ISimpleService>()
				.ImplementedBy<SimpleServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<int>>());
			container.Register(Component
				.For<ICompositeService>()
				.ImplementedBy<CompositeServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<string>>());

			using (new LifetimeScope<int>())
			{
				var simpleService = container.Resolve<ISimpleService>();
				using (new LifetimeScope<string>())
				{
					var compositeService = container.Resolve<ICompositeService>();
					Assert.AreEqual(simpleService, compositeService.SimpleService);
				}
			}
		}

		[Test]
		public void If_service_in_scope_has_dependecy_to_service_which_is_in_another_not_opened_contextual_scope_should_fail_to_wire_it()
		{
			var container = new WindsorContainer();
			container.Register(Component
				.For<ISimpleService>()
				.ImplementedBy<SimpleServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<int>>());
			container.Register(Component
				.For<ICompositeService>()
				.ImplementedBy<CompositeServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<string>>());

			using (new LifetimeScope<string>())
			{
				Assert.Throws<InvalidOperationException>(
					() => container.Resolve<ICompositeService>(),
					"Dependency could be resolved only when 'Int32' scope is opened.");
			}
		}

		[Test]
		public void Scopes_of_same_context_can_coexist_when_opened_in_different_threads_and_no_root_scope_of_same_context_in_parent_thread_exist()
		{
			var container = new WindsorContainer();
			container.Register(Component
				.For<ISimpleService>()
				.ImplementedBy<SimpleServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<int>>());

			var manualResetEvent = new ManualResetEvent(false);
			var tasks = Enumerable.Range(1, 100)
				.Select(i => new Task<ISimpleService>(() =>
				{
					manualResetEvent.WaitOne();
					using (new LifetimeScope<int>())
					{
						Thread.SpinWait(10000);
						return container.Resolve<ISimpleService>();
					}
				})).ToArray();
			foreach (var task in tasks)
			{
				task.Start();
			}
			manualResetEvent.Set();
			Task.WaitAll(tasks);

			var services = tasks.Select(t => t.Result).ToList();
			for (int i = 0; i < services.Count; i++)
			{
				for (int j = i + 1; j < services.Count; j++)
				{
					Assert.AreNotEqual(services[i], services[j]);
				}
			}
		}

		[Test]
		public void Scopes_of_same_context_cannot_coexist_when_opened_in_different_threads_and_already_opened_in_parent_thread()
		{
			using (new LifetimeScope<int>())
			{
				Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
				var task = Task.Factory.StartNew(() =>
				{
					using (new LifetimeScope<int>())
					{
						Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
					}
				});

				Assert.Throws<AggregateException>(
					task.Wait,
					"'Int32' scope is valid only as a root scope. Lifetime scopes do not support nested scoped of same context.");
			}
		}

		[Test]
		public void Should_resolve_dependecy_in_child_threads_when_scope_of_another_context_is_opened_in_parent_thread()
		{
			var container = new WindsorContainer();
			container.Register(Component
				.For<ISimpleService>()
				.ImplementedBy<SimpleServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<int>>());
			container.Register(Component
				.For<ICompositeService>()
				.ImplementedBy<CompositeServiceImpl>()
				.LifeStyle.Custom<PerLifetimeScopeLifestyle<string>>());

			using (new LifetimeScope<int>())
			{
				var simpleService = container.Resolve<ISimpleService>();
				var tasks = Enumerable.Range(1, 100)
					.Select(i => new Task<ICompositeService>(() =>
					{
						using (new LifetimeScope<string>())
						{
							return container.Resolve<ICompositeService>();
						}
					}))
					.ToArray();
				foreach (var task in tasks)
				{
					task.Start();
				}

				var compositeServices = tasks.Select(t => t.Result).ToList();
				foreach (var compositeService in compositeServices)
				{
					Assert.AreEqual(simpleService, compositeService.SimpleService);
				}
			}
		}
	}
}
