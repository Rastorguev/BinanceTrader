using System;

namespace BinanceTrader.Api
{
    public enum CandlesInterval
    {
        Minutes1,
        Minutes3,
        Minutes5,
        Minutes15,
        Minutes30,
        Hours1
    }

    public static class CandlesIntervalExtensions
    {
        public static int ToMinutes(this CandlesInterval interval)
        {
            switch (interval)
            {
                case CandlesInterval.Minutes1:
                    return 1;
                case CandlesInterval.Minutes3:
                    return 3;
                case CandlesInterval.Minutes5:
                    return 5;
                case CandlesInterval.Minutes15:
                    return 15;
                case CandlesInterval.Minutes30:
                    return 30;
                case CandlesInterval.Hours1:
                    return 60;
                default:
                    throw new ArgumentOutOfRangeException(nameof(interval), interval, null);
            }
        }
    }
}