using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Omni.Core
{
	public class Row : Dictionary<string, object>
	{
		public new object this[string key] => base[key];
		public T Get<T>(string key) => (T)Convert.ChangeType(base[key], typeof(T));
		public T FastGet<T>(string key)
		{
			object value = base[key];
			return Unsafe.As<object, T>(ref value);
		}

		public bool TryGet<T>(string key, out T prop)
		{
			prop = default;
			try
			{
				if (TryGetValue(key, out object value))
				{
					prop = (T)Convert.ChangeType(value, typeof(T));
					return true;
				}
				return false;
			}
			catch
			{
				return false;
			}
		}

		public bool TryFastGet<T>(string key, out T prop)
		{
			prop = default;
			try
			{
				if (TryGetValue(key, out object value))
				{
					prop = Unsafe.As<object, T>(ref value);
					return true;
				}
				return false;
			}
			catch
			{
				return false;
			}
		}

		public new void Add(string key, object value) => base.Add(key, value);
		public new bool TryAdd(string key, object value) => base.TryAdd(key, value);
	}
}