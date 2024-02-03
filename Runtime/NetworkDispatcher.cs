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

#pragma warning disable

using System;
using System.Collections.Generic;

namespace Omni.Internal.Transport
{
	// Thread-safe
	public class NetworkDispatcher
	{
		private readonly object _lock = new object();
		private readonly Queue<Action> m_queue = new Queue<Action>();

		public void Dispatch(Action action)
		{
			lock (_lock)
			{
				m_queue.Enqueue(action);
			}
		}

		internal void Process()
		{
			lock (_lock)
			{
				if (m_queue.Count > 0)
				{
					Action func = m_queue.Dequeue();
					func(); // run in Unity main thread!
				}
			}
		}
	}
}