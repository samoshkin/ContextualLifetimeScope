using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;

namespace ContextualLifetimeScope
{
	public class LifetimeScopeStore
	{
		private class LogicalThreadAffinativeDictionary : Dictionary<object, object>, ILogicalThreadAffinative
		{ }

		private readonly string _dataSlotKey;

		private LifetimeScopeStore(Type context)
		{
			Context = context;
			_dataSlotKey = context.FullName;
		}

		public Type Context { get; private set; }

		public static LifetimeScopeStore Get<TContext>()
		{
			return new LifetimeScopeStore(typeof(TContext));
		}

		/// <summary>
		/// Gets object by the key, if exists, otherwise, creates object and associate it with the key
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// Scope is not opened
		/// </exception>
		/// <param name="key"></param>
		/// <param name="createValue"></param>
		/// <returns></returns>
		public TValue GetOrAdd<TKey, TValue>(TKey key, Func<TKey, TValue> createValue)
		{
			lock(this)
			{
				LogicalThreadAffinativeDictionary state;
				VerifyScopeOpenness(true, out state);

				object value;
				if(state.ContainsKey(key))
				{
					value = state[key];
				}
				else
				{
					value = createValue(key);
					state.Add(key, value);
				}
				return value == null? default(TValue) : (TValue)value;
			}
		}

		public bool TryRemove(object key)
		{
			lock (this)
			{
				LogicalThreadAffinativeDictionary state;
				VerifyScopeOpenness(true, out state);

				return state.Remove(key);
			}
		}
		
		/// <summary>
		/// Opens scope and allocated data slot associated with scope
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// Scope is already opened
		/// </exception>
		public void OpenScope()
		{
			lock(this)
			{
				VerifyScopeOpenness(false);
				CallContext.SetData(_dataSlotKey, new LogicalThreadAffinativeDictionary());	
			}
		}

		/// <summary>
		/// Closes scope, releases associated data and frees CallContext data slot
		/// </summary>
		/// <remarks>
		/// If item stored is IDisposable, call Dispose() method on it
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Scope is not opened
		/// </exception>
		public void CloseScope()
		{
			lock (this)
			{
				LogicalThreadAffinativeDictionary state;
				VerifyScopeOpenness(true, out state);

				try
				{
					ReleaseItems(state);	
				}
				finally
				{
					CallContext.FreeNamedDataSlot(_dataSlotKey);		
				}
			}
		}

		private void ReleaseItems(LogicalThreadAffinativeDictionary state)
		{
			foreach (var item in state.Values.OfType<IDisposable>())
			{
				item.Dispose();
			}
			state.Clear();
		}

		private void VerifyScopeOpenness(bool shouldBeOpened)
		{
			LogicalThreadAffinativeDictionary state;
			VerifyScopeOpenness(shouldBeOpened, out state);
		}

		private void VerifyScopeOpenness(bool shouldBeOpened, out LogicalThreadAffinativeDictionary state)
		{
			state = CallContext.GetData(_dataSlotKey) as LogicalThreadAffinativeDictionary;
			bool isOpened = state != null;
			if (isOpened ^ shouldBeOpened)
			{
				var getStateInfo = new Func<bool, string>(opened => opened ? "opened" : "closed");
				throw new InvalidOperationException(String.Format(
					"Lifetime scope '{0}' state is '{1}', but should be '{2}'.",
					Context.FullName,
					getStateInfo(isOpened),
					getStateInfo(shouldBeOpened)));
			}
		}

		
	}
}
