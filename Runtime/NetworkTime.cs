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

using Omni.Internal;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Omni.Core
{
	[DefaultExecutionOrder(-5000)]
	public class NetworkTime : RealtimeTickBasedSystem
	{
		public class Clock : ITickData
		{
			private readonly TickModeOption tickModeOption;
			private readonly List<IRealtimeTickBasedSystem> handlers = new();

			private double deltaTickTime;
			private double lastDeltaTickTime;

			public long TotalTicks { get; private set; }
			public long CurrentTick { get; private set; }

			public double DeltaTime { get; private set; }
			public double DeltaTick { get; private set; }

			public int TickRate { get; }
			public double FixedTimestep { get; }

			internal List<IRealtimeTickBasedSystem> Handlers => handlers;
			internal Clock(int tickRate, TickModeOption tickModeOption)
			{
				this.tickModeOption = tickModeOption;
				TickRate = tickRate;
				FixedTimestep = 1.0d / tickRate;
			}

			internal void AddHandler(IRealtimeTickBasedSystem handler)
			{
				handlers.Add(handler);
			}

			internal void RemoveHandler(IRealtimeTickBasedSystem handler)
			{
				handlers.Remove(handler);
			}

			internal void OnTick()
			{
				DeltaTime += Time.deltaTime;
				deltaTickTime += Time.deltaTime;
				while (DeltaTime >= FixedTimestep)
				{
					// The interval in seconds from the last tick to the current one (Read Only).
					DeltaTick = deltaTickTime - lastDeltaTickTime;
					lastDeltaTickTime = deltaTickTime;
					// Add tick per frame(1 / tickrate)
					CurrentTick++;
					TotalTicks++;
					for (int i = 0; i < handlers.Count; i++)
					{
						IRealtimeTickBasedSystem handler = handlers[i];
						switch (tickModeOption)
						{
							case TickModeOption.Update:
								{
									if (CurrentTick == 1)
									{
										handler.OnUpdateTickStart(this);
									}
									handler.OnUpdateTick(this);
									if (CurrentTick == TickRate)
									{
										handler.OnUpdateTickEnd(this);
									}
								}
								break;
							case TickModeOption.FixedUpdate:
								if (CurrentTick == 1)
								{
									handler.OnFixedUpdateTickStart(this);
								}
								handler.OnFixedUpdateTick(this);
								if (CurrentTick == TickRate)
								{
									handler.OnFixedUpdateTickEnd(this);
								}
								break;
						}
					}

					if (CurrentTick == TickRate)
					{
						CurrentTick -= TickRate;
					}
					DeltaTime -= FixedTimestep;
				}
			}
		}

		public Clock UpdateClock { get; private set; }
		public Clock FixedUpdateClock { get; private set; }

		[SerializeField][Min(1)] private int updateTickRate = 15; // per second!
		[SerializeField][Min(1)] private int fixedUpdateTickRate = 50; // per second!

		public double ServerTime => OmniNetwork.Ntp.SynchronizedTime;
		public double Latency
		{
			get
			{
				return OmniNetwork.Main.LoopMode == GameLoopOption.TickBased
					? (int)Math.Max(0, (MathHelper.MinMax(OmniNetwork.Ntp.Latency, 1.035d) * UpdateClock.FixedTimestep / 2 * 1000.0d) - (UpdateClock.DeltaTick * UpdateClock.FixedTimestep / 2 * 1000.0d))
					: (int)Math.Max(0, (MathHelper.MinMax(OmniNetwork.Ntp.Latency, 0.035d) / 2 * 1000.0d) - (Time.deltaTime / 2 * 1000.0d));
			}
		}

		public int UpdateTickRate => updateTickRate;
		public int FixedUpdateTickRate => fixedUpdateTickRate;

		private void Awake()
		{
			UpdateClock = new Clock(updateTickRate, TickModeOption.Update);
			FixedUpdateClock = new Clock(fixedUpdateTickRate, TickModeOption.FixedUpdate);
		}

		public override void Start()
		{
			base.Start();
		}

		private void Update()
		{
			if (OmniNetwork.Main.LoopMode == GameLoopOption.TickBased)
			{
				UpdateClock.OnTick();
			}
			else
			{
				for (int i = 0; i < UpdateClock.Handlers.Count; i++)
				{
					IRealtimeTickBasedSystem handler = UpdateClock.Handlers[i];
					handler.OnUpdate();
				}
			}
		}

		private void FixedUpdate()
		{
			if (OmniNetwork.Main.LoopMode == GameLoopOption.TickBased)
			{
				FixedUpdateClock.OnTick();
			}
			else
			{
				for (int i = 0; i < UpdateClock.Handlers.Count; i++)
				{
					IRealtimeTickBasedSystem handler = UpdateClock.Handlers[i];
					handler.OnFixedUpdate();
				}
			}
		}
	}
}