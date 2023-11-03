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
using static Omni.Core.Enums;

namespace Omni.Core
{
    [Serializable]
    public class SyncValue<T> : SyncBase<T> where T : unmanaged
    {
        public SyncValue(OmniObject @this, T value = default, Action<T> onChanged = null, Channel channel = Channel.Unreliable, Target target = Target.All, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None, AuthorityMode authority = AuthorityMode.Server) : base(@this, value, channel, target, subTarget, cacheMode, authority)
        {
            Type _ = value.GetType();
            if (_.IsEnum)
                throw new Exception($"Use \"SyncValue<Enum, T>\" instead \"SyncValue<T>\"");
            if (_ == typeof(Trigger))
                throw new Exception($"Use \"SyncValue\" instead \"SyncValue<T>\"");

            @this.OnSyncBase += (id, message) =>
            {
                if (this.id == id)
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
        public SyncValue(OmniObject @this, Action onChanged, Channel channel = Channel.Unreliable, Target target = Target.All, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None, AuthorityMode authority = AuthorityMode.Server) : base(@this, default, channel, target, subTarget, cacheMode, authority)
        {
            @this.OnSyncBase += (id, message) =>
            {
                if (this.id == id)
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
        public SyncValue(OmniObject @this, Enum value = default, Action<Enum> onChanged = null, Channel channel = Channel.Unreliable, Target target = Target.All, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None, AuthorityMode authority = AuthorityMode.Server) : base(@this, (T)Convert.ChangeType(value, typeof(T)), channel, target, subTarget, cacheMode, authority, value)
        {
            @this.OnSyncBase += (id, message) =>
            {
                if (this.id == id)
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