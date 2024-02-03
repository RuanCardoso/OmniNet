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
	public class SyncValueCustom<T> : SyncBase<T> where T : unmanaged, ISyncCustom
	{
		public SyncValueCustom(NetworkIdentity identity, T value = default, Action<T> onChanged = null, DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured, DataTarget target = DataTarget.Broadcast, DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer, DataCachingOption cachingOption = DataCachingOption.None, AuthorityMode authority = AuthorityMode.Server) : base(identity, value, deliveryMode, target, processingOption, cachingOption, authority, value)
		{
			if (identity == null)
			{
				OmniLogger.PrintError("Error: SyncVar -> The provided NetworkIdentity is null.");
				return;
			}

			identity.OnSyncBase += (id, message) =>
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