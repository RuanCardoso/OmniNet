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
    public class SyncRef<T> : SyncBase<T> where T : class
    {
        public SyncRef(OmniObject @this, T value, Action<T> onChanged = null, Channel channel = Channel.Unreliable, Target target = Target.All, SubTarget subTarget = SubTarget.None, CacheMode cacheMode = CacheMode.None, AuthorityMode authority = AuthorityMode.Server) : base(@this, value, channel, target, subTarget, cacheMode, authority)
        {
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
}