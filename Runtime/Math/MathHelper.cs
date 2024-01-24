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

namespace Omni.Internal
{
	public static class MathHelper
	{
		public static double Clamp01(double value)
		{
			if (value < 0d)
				return 0d;
			else if (value > 1d)
				return 1d;
			else
				return value;
		}

		public static double Lerp(double a, double b, double t)
		{
			return a + (b - a) * Clamp01(t);
		}

		public static double MinMax(double value, double min)
		{
			return value < min ? 0 : value;
		}
	}
}
