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
using System.Threading.Tasks;

namespace Omni.Core
{
	public class ConcurrentDatabaseManager
	{
		private Func<bool, Task<Database>> builder;

		public void Initialize(Func<Database, Task<Database>> builder)
		{
			this.builder = (flushTemporaryConnection) =>
			{
				return builder?.Invoke(new Database());
			};
		}

		public Task<Database> GetAsync()
		{
			return builder(true);
		}
	}
}