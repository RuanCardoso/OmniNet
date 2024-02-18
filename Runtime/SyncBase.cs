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
using UnityEngine;

namespace Omni.Core
{
	[Serializable]
	public class SyncBase<T> : ISyncBase, ISyncBaseValue<T>
	{
		internal static readonly IValueTypeConverter<T> Converter = ValueTypeConverter._self as IValueTypeConverter<T>;

		public byte Id { get; private set; }
		public TypeCode TypeCode { get; }

		private Enum enumType;
		[SerializeField]
		private T value = default;

		private readonly bool isStruct;
		private readonly bool isReferenceType;
		private readonly bool isValueTypeSupported;
		private readonly NetworkBehaviour behaviour;
		private readonly ISyncCustom ISerialize;

		public SyncBase(NetworkBehaviour behaviour, T value, Enum enumType = null)
		{
			this.enumType = enumType;
			this.value = value;
			this.behaviour = behaviour;

			#region Initialize
			SetId(behaviour);
			var type = value.GetType();
			isStruct = type.IsValueType;
			isReferenceType = !type.IsValueType;
			isValueTypeSupported = ValueTypeConverter.types.Contains(type);
			TypeCode = Type.GetTypeCode(type);
			#endregion
		}

		public SyncBase(NetworkBehaviour behaviour, T value, ISyncCustom ISerialize)
		{
			this.value = value;
			this.behaviour = behaviour;
			this.ISerialize = ISerialize;

			#region Initialize
			SetId(behaviour);
			var type = value.GetType();
			isStruct = type.IsValueType;
			isReferenceType = false;
			isValueTypeSupported = false;
			TypeCode = Type.GetTypeCode(type);
			#endregion
		}

		private void SetId(NetworkBehaviour behaviour)
		{
			if (behaviour.OnSyncBaseId < byte.MaxValue)
			{
				Id = ++behaviour.OnSyncBaseId;
			}
			else
			{
				OmniLogger.PrintError("One identity only allows 255 syncvars, use another identity.");
			}
		}

		void ISyncBase.SetEnum(Enum enumValue)
		{
			if (enumType != enumValue)
			{
				enumType = enumValue;
				int value = Convert.ToInt32(enumType);
				SendEnumToNetwork((T)Convert.ChangeType(value, typeof(T)));
			}
		}

		private void UpdateEnum(T value)
		{
#if UNITY_EDITOR
			if (enumType != null)
				enumType = (Enum)Enum.ToObject(enumType.GetType(), value);
#endif
		}

		Enum ISyncBase.GetEnum() => enumType;
		bool ISyncBase.IsEnum() => enumType != null;
		void ISyncBase.OnValueChanged() => SyncOnNetwork();
		void ISyncBaseValue<T>.Intern_Set(T value)
		{
			UpdateEnum(value);
			this.value = value;
		}

		private void SendEnumToNetwork(T value)
		{
			this.value = value;
			SyncOnNetwork();
		}

		public T Get() => value;
		public void Set(T value)
		{
			UpdateEnum(value);
			this.value = value;
			SyncOnNetwork();
		}

		public void SetIfChanged(T value)
		{
			if (!value.Equals(this.value))
			{
				UpdateEnum(value);
				this.value = value;
				SyncOnNetwork();
			}
		}

		private void SyncOnNetwork()
		{
			if (!Application.isPlaying)
				return;

			if (behaviour == null)
			{
				OmniLogger.PrintError($"Error: It seems that the variable {GetType().Name} is not properly initialized. Please check if it's missing initialization.");
				return;
			}

			IDataWriter writer = NetworkCommunicator.DataWriterPool.Get();
			if (!isReferenceType)
			{
				if (isValueTypeSupported)
				{
					try
					{
						switch (TypeCode)
						{
							case TypeCode.Int32:
								writer.Write(Converter.GetInt(value));
								break;
							case TypeCode.Boolean:
								writer.Write(Converter.GetBool(value));
								break;
							case TypeCode.Single:
								writer.Write(Converter.GetFloat(value));
								break;
							case TypeCode.Byte:
								writer.Write(Converter.GetByte(value));
								break;
							default:
								writer.Write((byte)0x00000000);
								break;
						}
					}
					catch (NullReferenceException)
					{
						OmniLogger.PrintError($"{TypeCode} converter not implemented!");
					}
				}
				else
				{
					ISyncCustom ISerialize = isStruct ? (ISyncCustom)Get() : this.ISerialize;
					if (ISerialize != null) ISerialize.Serialize(writer);
					else OmniLogger.PrintError($"SyncValue -> Custom type is not supported, use {nameof(ISyncCustom)} instead!");
				}
			}
			else writer.SerializeWithMsgPack(value, null);
			//@this.SentOnSyncBase(Id, writer, HasAuthority, deliveryMode, target, processingOption, cachingOption);
			NetworkCommunicator.DataWriterPool.Release(writer);
		}

		public static implicit operator T(SyncBase<T> value) => value.value;
		public override bool Equals(object obj) => ((T)obj).Equals(value);
		public override int GetHashCode() => value.GetHashCode();
		public override string ToString() => value.ToString();
	}
}