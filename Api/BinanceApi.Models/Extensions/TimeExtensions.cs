using System;
using BinanceApi.Models.Enums;

namespace BinanceApi.Models.Extensions
{
    public static class TimeExtensions
    {
        public static DateTime GetUtcTime(this long unixTime)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(unixTime).DateTime;
        }

        public static DateTime GetLocalTime(this long unixTime)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(unixTime).DateTime.ToLocalTime();
        }

        public static TimeSpan ToTimeSpan(this TimeInterval interval)
        {
            switch (interval)
            {
                case TimeInterval.Seconds_1:
                    return TimeSpan.FromSeconds(1);
                case TimeInterval.Minutes_1:
                    return  TimeSpan.FromMinutes(1);
                case TimeInterval.Minutes_3:
                    return TimeSpan.FromMinutes(3);
                case TimeInterval.Minutes_5:
                    return TimeSpan.FromMinutes(5);;
                case TimeInterval.Minutes_15:
                    return TimeSpan.FromMinutes(15);;
                case TimeInterval.Minutes_30:
                    return TimeSpan.FromMinutes(30);
                case TimeInterval.Hours_1:
                    return TimeSpan.FromHours(1);;
                default:
                    throw new ArgumentOutOfRangeException(nameof(interval), interval, null);
            }
        }
    }
}