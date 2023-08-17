using System;
using Binance.API.Csharp.Client.Models.Enums;

namespace Binance.API.Csharp.Client.Models.Extensions
{
    public static class TimeExtensions
    {
        public static DateTime GetTime(this long unixTime)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(unixTime).DateTime;
        }

        public static int ToMinutes(this TimeInterval interval)
        {
            switch (interval)
            {
                case TimeInterval.Minutes_1:
                    return 1;
                case TimeInterval.Minutes_3:
                    return 3;
                case TimeInterval.Minutes_5:
                    return 5;
                case TimeInterval.Minutes_15:
                    return 15;
                case TimeInterval.Minutes_30:
                    return 30;
                case TimeInterval.Hours_1:
                    return 60;
                default:
                    throw new ArgumentOutOfRangeException(nameof(interval), interval, null);
            }
        }
    }
}