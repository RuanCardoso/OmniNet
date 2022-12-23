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

using NaughtyAttributes;
using System;
using UnityEngine;
using static Neutron.Core.Enums;

namespace Neutron.Core
{
    public class SyncBase<T>
    {
        private static readonly IValueTypeConverter<T> Converter = ValueTypeConverter._ as IValueTypeConverter<T>;

        [SerializeField]
        [AllowNesting]
        [Label("<------>")]
        [OnValueChanged(nameof(OnEditorSet))] private T value = default;

        private bool HasAuthority => authority switch
        {
            AuthorityMode.Mine => @this.IsMine,
            AuthorityMode.Server => @this.IsServer,
            AuthorityMode.Client => @this.IsClient,
            AuthorityMode.Free => @this.IsFree,
            _ => default,
        };

        private readonly byte id;
        private readonly bool isReferenceType;
        private readonly bool isValueTypeSupported;
        private readonly NeutronObject @this;
        private readonly TypeCode typeCode;
        private readonly ISerializeValueType ISerialize;
        private readonly Channel channel;
        private readonly Target target;
        private readonly SubTarget subTarget;
        private readonly CacheMode cacheMode;
        private readonly AuthorityMode authority;
        public SyncBase(NeutronObject @this, T value, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, AuthorityMode authority)
        {
            this.value = value;
            this.@this = @this;
            this.channel = channel;
            this.target = target;
            this.subTarget = subTarget;
            this.cacheMode = cacheMode;
            this.authority = authority;
            id = @this.SYNC_BASE_ID++;
            // Determine if the value is reference type or value type!
            var type = value.GetType();
            isReferenceType = !type.IsValueType;
            isValueTypeSupported = ValueTypeConverter.Types.Contains(type);
            typeCode = Type.GetTypeCode(type);
            // Sync when the value is changed for the first time, i.e. on assignment.
            //........
        }

        public SyncBase(NeutronObject @this, T value, Channel channel, Target target, SubTarget subTarget, CacheMode cacheMode, AuthorityMode authority, ISerializeValueType ISerialize = default)
        {
            this.value = value;
            this.@this = @this;
            this.ISerialize = ISerialize;
            this.channel = channel;
            this.target = target;
            this.subTarget = subTarget;
            this.cacheMode = cacheMode;
            this.authority = authority;
            id = @this.SYNC_BASE_ID++;
            // Determine if the value is reference type or value type!
            var type = value.GetType();
            isReferenceType = false;
            isValueTypeSupported = false;
            typeCode = Type.GetTypeCode(type);
            // Sync when the value is changed for the first time, i.e. on assignment.
            //........
        }

        public void Set(T value)
        {
            this.value = value;
            SyncOnNetwork();
        }

        public T Get() => value;
        private void OnEditorSet() => SyncOnNetwork();
        private void SyncOnNetwork()
        {
            ByteStream message = ByteStream.Get();
            if (!isReferenceType)
            {
                if (isValueTypeSupported)
                {
                    try
                    {
                        switch (typeCode)
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
                        }
                    }
                    catch (NullReferenceException)
                    {
                        Logger.PrintError($"{typeCode} converter not implemented!");
                    }
                }
                else
                {
                    if (ISerialize != null)
                        ISerialize.Serialize(message);
                    else Logger.PrintError("SyncValue -> Custom type is not supported, use SyncCustom instead!");
                }
            }
            else message.Serialize(value);
            // Sent this var to Network......
            @this.SentOnSyncBase(id, message, HasAuthority, channel, target, subTarget, cacheMode);
        }

        public static implicit operator T(SyncBase<T> value) => value.value;
        public override bool Equals(object obj) => ((T)obj).Equals(value);
        public override int GetHashCode() => value.GetHashCode();
        public override string ToString() => value.ToString();
    }
}