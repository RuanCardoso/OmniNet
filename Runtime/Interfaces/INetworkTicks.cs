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

namespace Omni.Core
{
	public interface ITickData
	{
		public int TickRate { get; }
		public long TotalTicks { get; }
		public long CurrentTick { get; }
		public double FixedTimestep { get; }
		public double DeltaTime { get; }
		public double DeltaTick { get; }
	}

	public interface IRealtimeTickBasedSystem
	{
		void OnUpdate();
		void OnFixedUpdate();
		void OnUpdateTick(ITickData data);
		void OnUpdateTickStart(ITickData data);
		void OnUpdateTickEnd(ITickData data);
		void OnFixedUpdateTick(ITickData data);
		void OnFixedUpdateTickStart(ITickData data);
		void OnFixedUpdateTickEnd(ITickData data);
	}
}