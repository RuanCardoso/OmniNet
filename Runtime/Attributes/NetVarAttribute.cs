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
	// Roslyn Generated //  Roslyn Analyzer
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class NetVarAttribute : DrawerAttribute
	{
		public bool SerializeAsJson { get; set; }
		public bool CustomSerializeAndDeserialize { get; set; }
	}
}