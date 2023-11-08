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
    public class SyncRefCustom<T> : SyncBase<T> where T : class, ISyncCustom
    {
        public SyncRefCustom(OmniObject @this, T value, Action<T> onChanged = null, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget. Broadcast, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None, AuthorityMode authority = AuthorityMode.Server) : base(@this, value, deliveryMode, target, processingOption, cachingOption, authority, value)
        {
            @this.OnSyncBase += (id, message) =>
            {
                if (this.Id == id)
                {
                    this.Read(message);
                    onChanged?.Invoke(Get());
                }
            };
        }
    }
}