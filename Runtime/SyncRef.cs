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
	public class SyncRef<T> : SyncBase<T> where T : class
	{
		public SyncRef(NetworkBehaviour behaviour, T value, Action<T> onChanged) : base(behaviour, value)
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
					onChanged?.Invoke(Get());
				}
			};
		}
	}
}