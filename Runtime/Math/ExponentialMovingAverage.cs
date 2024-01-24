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
	public class ExponentialMovingAverage
	{
		private readonly float alpha;
		private bool isInitialized;
		private double avg;

		public ExponentialMovingAverage(int periods)
		{
			alpha = 2.0f / (periods + 1);
			isInitialized = false;
			avg = 0;
		}

		public void Add(double value)
		{
			if (isInitialized)
			{
				double delta = value - avg;
				avg += alpha * delta;
			}
			else
			{
				avg = value;
				isInitialized = true;
			}
		}

		public double GetAverage()
		{
			return avg;
		}
	}
}
