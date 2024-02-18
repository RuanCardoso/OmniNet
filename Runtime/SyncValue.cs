/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

using System;

namespace Omni.Core
{
	[Serializable]
	public class SyncValue<T> : SyncBase<T> where T : unmanaged
	{
		public SyncValue(NetworkBehaviour behaviour, T value, Action<T> onChanged) : base(behaviour, value)
		{
			if (behaviour == null)
			{
				OmniLogger.PrintError("Error: SyncVar -> The provided NetworkIdentity is null.");
				return;
			}

			Type _ = value.GetType();
			if (_.IsEnum)
				throw new Exception($"Use \"SyncValue<Enum, T>\" instead \"SyncValue<T>\"");
			if (_ == typeof(Trigger))
				throw new Exception($"Use \"SyncValue\" instead \"SyncValue<T>\"");

			behaviour.OnSyncBase += (id, message) =>
			{
				if (this.Id == id)
				{
					this.Read(message);
					onChanged?.Invoke(Get());
				}
			};
		}
	}

	[Serializable]
	public class SyncValue : SyncBase<Trigger>
	{
		public SyncValue(NetworkBehaviour behaviour, Action onChanged) : base(behaviour, default)
		{
			if (behaviour == null)
			{
				OmniLogger.PrintError("Error: SyncVar -> The provided NetworkIdentity is null.");
				return;
			}

			behaviour.OnSyncBase += (id, message) =>
			{
				if (this.Id == id)
				{
					this.Read(message);
					onChanged?.Invoke();
				}
			};
		}

		public void Set() => base.Set(default);
		public new void SetIfChanged(Trigger value) { throw new NotImplementedException(); }
		public new void Set(Trigger _) { throw new NotImplementedException(); }
		public new Trigger Get() { throw new NotImplementedException(); }
	}

	[Serializable]
	public class SyncValue<Enum, T> : SyncBase<T>
		where Enum : System.Enum
		where T : unmanaged
	{
		public T Value => base.Get();
		public SyncValue(NetworkBehaviour behaviour, Enum value = default, Action<Enum> onChanged = null) : base(behaviour, (T)Convert.ChangeType(value, typeof(T)), value)
		{
			if (behaviour == null)
			{
				OmniLogger.PrintError("Error: SyncVar -> The provided NetworkIdentity is null.");
				return;
			}

			behaviour.OnSyncBase += (id, message) =>
			{
				if (this.Id == id)
				{
					this.Read(message);
					onChanged?.Invoke(ToEnum());
				}
			};
		}

		public static implicit operator Enum(SyncValue<Enum, T> value) => value.ToEnum();
		public void Set(Enum value) => Set((T)Convert.ChangeType(value, typeof(T)));
		public void SetIfChanged(Enum value) => SetIfChanged((T)Convert.ChangeType(value, typeof(T)));
		public new Enum Get() => ToEnum();
		private Enum ToEnum() => (Enum)System.Enum.ToObject(typeof(Enum), base.Get());
	}
}