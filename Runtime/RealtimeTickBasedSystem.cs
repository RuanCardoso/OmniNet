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

using UnityEngine;

namespace Omni.Core
{
	public class RealtimeTickBasedSystem : MonoBehaviour, IRealtimeTickBasedSystem
	{
		public virtual void Start()
		{
			OmniNetwork.Time.UpdateClock.AddHandler(this);
			OmniNetwork.Time.FixedUpdateClock.AddHandler(this);
		}

		public virtual void OnFixedUpdateTick(ITickData tick) { }
		public virtual void OnFixedUpdateTickEnd(ITickData tick) { }
		public virtual void OnFixedUpdateTickStart(ITickData tick) { }
		public virtual void OnUpdateTick(ITickData tick) { }
		public virtual void OnUpdateTickEnd(ITickData tick) { }
		public virtual void OnUpdateTickStart(ITickData tick) { }
		public virtual void OnUpdate() { }
		public virtual void OnFixedUpdate() { }
	}
}