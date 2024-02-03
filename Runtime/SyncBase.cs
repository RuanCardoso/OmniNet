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
		[SerializeField] private T value = default;
		private bool HasAuthority => authority switch
		{
			//AuthorityMode.Mine => @this.IsMine,
			//AuthorityMode.Server => @this.IsServer,
			//AuthorityMode.Client => @this.IsClient,
			//AuthorityMode.Custom => @this.IsCustom,
			_ => default,
		};

		private readonly bool isStruct;
		private readonly bool isReferenceType;
		private readonly bool isValueTypeSupported;
		private readonly NetworkIdentity identity;
		private readonly ISyncCustom ISerialize;
		private readonly DataDeliveryMode deliveryMode;
		private readonly DataTarget target;
		private readonly DataProcessingOption processingOption;
		private readonly DataCachingOption cachingOption;
		private readonly AuthorityMode authority;
		public SyncBase(NetworkIdentity @this, T value, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, AuthorityMode authority, Enum enumType = null)
		{
			this.enumType = enumType;
			this.value = value;
			this.identity = @this;
			this.deliveryMode = deliveryMode;
			this.target = target;
			this.processingOption = processingOption;
			this.cachingOption = cachingOption;
			this.authority = authority;
			SetId(@this);
			var type = value.GetType();
			isStruct = type.IsValueType;
			isReferenceType = !type.IsValueType;
			isValueTypeSupported = ValueTypeConverter.types.Contains(type);
			TypeCode = Type.GetTypeCode(type);
		}

		public SyncBase(NetworkIdentity identity, T value, DataDeliveryMode deliveryMode, DataTarget target, DataProcessingOption processingOption, DataCachingOption cachingOption, AuthorityMode authority, ISyncCustom ISerialize)
		{
			this.value = value;
			this.identity = identity;
			this.ISerialize = ISerialize;
			this.deliveryMode = deliveryMode;
			this.target = target;
			this.processingOption = processingOption;
			this.cachingOption = cachingOption;
			this.authority = authority;
			SetId(identity);
			var type = value.GetType();
			isStruct = type.IsValueType;
			isReferenceType = false;
			isValueTypeSupported = false;
			TypeCode = Type.GetTypeCode(type);
		}

		private void SetId(NetworkIdentity identity)
		{
			if (identity == null)
			{
				OmniLogger.PrintError("Error: SyncVar -> The provided NetworkIdentity is null.");
				return;
			}

			// Note: Byte allow only 255 SyncVars per identity.
			// Note: Use another identity if you need more.
			if (identity.OnSyncBaseId < byte.MaxValue)
			{
				Id = ++identity.OnSyncBaseId;
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

			if (identity == null)
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