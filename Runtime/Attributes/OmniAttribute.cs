using System;

namespace Omni.Core
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class OmniAttribute : Attribute
	{
#pragma warning disable IDE0060
		public OmniAttribute(params string[] @params)
#pragma warning restore IDE0060
		{
		}
	}
}
