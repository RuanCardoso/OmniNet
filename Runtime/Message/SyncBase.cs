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
using static Omni.Core.Enums;

namespace Omni.Core
{
    [Serializable]
    public class SyncBase<T> : ISyncBase, ISyncBaseValue<T>
    {
        internal protected readonly byte id;
        internal static readonly IValueTypeConverter<T> Converter = ValueTypeConverter._self as IValueTypeConverter<T>;

        public TypeCode TypeCode { get; }
        private Enum enumType;

        [SerializeField] private T value = default;
        private bool HasAuthority => authority switch
        {
            AuthorityMode.Mine => @this.IsMine,
            AuthorityMode.Server => @this.IsServer,
            AuthorityMode.Client => @this.IsClient,
            AuthorityMode.Custom => @this.IsCustom,
            _ => default,
        };

        private readonly bool isStruct;
        private readonly bool isReferenceType;
        private readonly bool isValueTypeSupported;
        private readonly OmniObject @this;
        private readonly ISyncCustom ISerialize;
        private readonly Channel channel;
        private readonly Target target;
        private readonly SubTarget subTarget;
        private readonly CacheMode cacheMode;
        private readonly AuthorityMode authority;
        public SyncBase(OmniObject @this, T value, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, AuthorityMode authority, Enum enumType = null)
        {
            this.enumType = enumType;
            this.value = value;
            this.@this = @this;
            this.channel = channel;
            this.target = target;
            this.subTarget = subTarget;
            this.cacheMode = cacheMode;
            this.authority = authority;
            id = @this.OnSyncBaseId++;
            // Determine if the value is reference type or value type!
            var type = value.GetType();
            isStruct = type.IsValueType;
            isReferenceType = !type.IsValueType;
            isValueTypeSupported = ValueTypeConverter.types.Contains(type);
            TypeCode = Type.GetTypeCode(type);
        }

        public SyncBase(OmniObject @this, T value, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, AuthorityMode authority, ISyncCustom ISerialize)
        {
            this.value = value;
            this.@this = @this;
            this.ISerialize = ISerialize;
            this.channel = channel;
            this.target = target;
            this.subTarget = subTarget;
            this.cacheMode = cacheMode;
            this.authority = authority;
            id = @this.OnSyncBaseId++;
            // Determine if the value is reference type or value type!
            var type = value.GetType();
            isStruct = type.IsValueType;
            isReferenceType = false;
            isValueTypeSupported = false;
            TypeCode = Type.GetTypeCode(type);
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

            if (@this == null)
            {
                Logger.PrintError($"did you forget to initialize the variable? -> {GetType().Name}");
                return;
            }

            ByteStream message = ByteStream.Get();
            if (!isReferenceType)
            {
                if (isValueTypeSupported)
                {
                    try
                    {
                        switch (TypeCode)
                        {
                            case TypeCode.Int32:
                                message.Write(Converter.GetInt(value));
                                break;
                            case TypeCode.Boolean:
                                message.Write(Converter.GetBool(value));
                                break;
                            case TypeCode.Single:
                                message.Write(Converter.GetFloat(value));
                                break;
                            case TypeCode.Byte:
                                message.Write(Converter.GetByte(value));
                                break;
                            default:
                                message.Write((byte)0x00000000);
                                break;
                        }
                    }
                    catch (NullReferenceException)
                    {
                        Logger.PrintError($"{TypeCode} converter not implemented!");
                    }
                }
                else
                {
                    ISyncCustom ISerialize = isStruct ? (ISyncCustom)Get() : this.ISerialize;
                    if (ISerialize != null) ISerialize.Serialize(message);
                    else Logger.PrintError($"SyncValue -> Custom type is not supported, use {nameof(ISyncCustom)} instead!");
                }
            }
            else message.SerializeWithMsgPack(value);
            @this.SentOnSyncBase(id, message, HasAuthority, channel, target, subTarget, cacheMode);
        }

        public static implicit operator T(SyncBase<T> value) => value.value;
        public override bool Equals(object obj) => ((T)obj).Equals(value);
        public override int GetHashCode() => value.GetHashCode();
        public override string ToString() => value.ToString();
    }
}