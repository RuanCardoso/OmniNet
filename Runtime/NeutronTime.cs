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

namespace Neutron.Core
{
    public static class NeutronTime
    {
        private const int WINDOW_SIZE = 10;
        private static double _recMsgs = 1d;
        private static double _sentMsgs = 1d;
        private static ExponentialMovingAverage _rttExAvg = new(WINDOW_SIZE);
        private static ExponentialMovingAverage _offsetExAvg = new(WINDOW_SIZE);
        private static double _offsetMin = double.MinValue;
        private static double _offsetMax = double.MaxValue;

        public static double PacketLoss => Math.Abs(Math.Round(100d - (_recMsgs / _sentMsgs * 100d), MidpointRounding.ToEven));
        public static double Latency => Math.Round((RoundTripTime * 0.5d) * 1000d);
        public static double Ping => Math.Round(RoundTripTime * 1000d);
        public static double RoundTripTime => _rttExAvg.Avg;
        public static double Offset => _offsetExAvg.Avg;
        public static double LocalTime => NeutronNetwork.timeAsDouble;
        public static double Time => LocalTime - Offset;
        public static double RttSlope => _rttExAvg.Slope;
        public static double OffsetSlope => _offsetExAvg.Slope;

        public static void SetTime(double clientTime, double serverTime)
        {
            double now = LocalTime;
            double rtt = now - clientTime;
            double halfRtt = rtt * 0.5d;
            double offset = now - halfRtt - serverTime;
            double offsetMin = now - rtt - serverTime;
            double offsetMax = now - serverTime;

            _offsetMin = Math.Max(_offsetMin, offsetMin);
            _offsetMax = Math.Min(_offsetMax, offsetMax);

            _rttExAvg.Add(rtt);
            if (_offsetExAvg.Avg < _offsetMin || _offsetExAvg.Avg > _offsetMax)
            {
                _offsetExAvg = new ExponentialMovingAverage(WINDOW_SIZE);
                _offsetExAvg.Add(offset);
            }
            else if (offset >= _offsetMin || offset <= _offsetMax)
                _offsetExAvg.Add(offset);
        }

        public static void AddSent() => _sentMsgs++;
        public static void AddReceived() => _recMsgs++;
    }
}