/*===========================================================
    Author: Ruan Cardoso, Vis2k(Mirror)
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
    -
    Thanks: https://github.com/vis2k/Mirror/blob/master/Assets/Mirror/Runtime/NetworkTime.cs
    ===========================================================*/

using System;
using InternalTime = UnityEngine.Time;

namespace Omni.Core
{
    public static class OmniTime
    {
        private const int WINDOW_SIZE = 10;

        private static ExponentialMovingAverage _rttExAvg = new(WINDOW_SIZE);
        private static ExponentialMovingAverage _offsetExAvg = new(WINDOW_SIZE);

        private static double offsetMin = double.MinValue;
        private static double offsetMax = double.MaxValue;

        public static double Latency => Math.Round(RoundTripTime * 0.5d * 1000d);
        public static double Ping => Math.Round(RoundTripTime * 1000d);
        public static double RoundTripTime => _rttExAvg.Avg;
        public static double Offset => _offsetExAvg.Avg;
        public static double LocalTime => InternalTime.timeAsDouble;
        public static double Time => LocalTime - Offset;
        public static double RttSlope => _rttExAvg.Slope;
        public static double OffsetSlope => _offsetExAvg.Slope;

        internal static void SetTime(double clientTime, double serverTime)
        {
            double now = Math.Max(0, LocalTime - ((double)InternalTime.deltaTime));
            double rtt = now - clientTime;
            double halfRtt = rtt * 0.5d;
            double offset = now - halfRtt - serverTime;
            double offsetMin = now - rtt - serverTime;
            double offsetMax = now - serverTime;

            OmniTime.offsetMin = Math.Max(OmniTime.offsetMin, offsetMin);
            OmniTime.offsetMax = Math.Min(OmniTime.offsetMax, offsetMax);

            _rttExAvg.Add(rtt);
            if (_offsetExAvg.Avg < OmniTime.offsetMin || _offsetExAvg.Avg > OmniTime.offsetMax)
            {
                _offsetExAvg = new ExponentialMovingAverage(WINDOW_SIZE);
                _offsetExAvg.Add(offset);
            }
            else if (offset >= OmniTime.offsetMin || offset <= OmniTime.offsetMax)
            {
                _offsetExAvg.Add(offset);
            }
        }
    }
}