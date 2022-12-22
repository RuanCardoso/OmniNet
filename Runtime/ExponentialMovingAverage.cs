/*===========================================================
    Author: Vis2k(Mirror)
    -
    License: Open Source (MIT)
    -
    Thanks: https://github.com/vis2k/Mirror/blob/master/Assets/Mirror/Runtime/ExponentialMovingAverage.cs
    ===========================================================*/

namespace Neutron.Core
{
    public struct ExponentialMovingAverage
    {
        readonly float alpha;
        bool initialized;

        public double Avg;
        public double Slope;

        public ExponentialMovingAverage(int n)
        {
            // standard N-day EMA alpha calculation
            alpha = 2.0f / (n + 1);
            initialized = false;
            Avg = 0;
            Slope = 0;
        }

        public void Add(double newValue)
        {
            // simple algorithm for EMA described here:
            // https://en.wikipedia.org/wiki/Moving_average#Exponentially_weighted_moving_variance_and_standard_deviation
            if (initialized)
            {
                double delta = newValue - Avg;
                Avg += alpha * delta;
                Slope = (1 - alpha) * (Slope + alpha * delta * delta);
            }
            else
            {
                Avg = newValue;
                initialized = true;
            }
        }
    }
}