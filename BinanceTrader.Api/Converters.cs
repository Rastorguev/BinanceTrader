using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BinanceTrader.Core.Entities;
using BinanceTrader.Core.Entities.Enums;
using JetBrains.Annotations;

namespace BinanceTrader.Api
{
    public static class Converters
    {
        public static string ToRequestParam(this OrderSide type)
        {
            switch (type)
            {
                case OrderSide.Buy:
                    return "BUY";
                case OrderSide.Sell:
                    return "SELL";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static string ToRequestParam(this TimeInForceType type)
        {
            switch (type)
            {
                case TimeInForceType.GTC:
                    return "GTC";
                case TimeInForceType.IOC:
                    return "IOC";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static string ToRequestParam(this OrderType type)
        {
            switch (type)
            {
                case OrderType.Limit:
                    return "LIMIT";
                case OrderType.Market:
                    return "MARKET";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static List<Candle> ToCandles([NotNull] this List<List<object>> rawCandles)
        {
            var candles = rawCandles.Select(rc =>
                new Candle
                {
                    OpenTime = DateTimeOffset.FromUnixTimeMilliseconds((long) rc[0]).LocalDateTime,
                    CloseTime = DateTimeOffset.FromUnixTimeMilliseconds((long) rc[6]).LocalDateTime,
                    OpenPrice = decimal.Parse((string) rc[1], CultureInfo.InvariantCulture),
                    ClosePrice = decimal.Parse((string) rc[4], CultureInfo.InvariantCulture),
                    HighPrice = decimal.Parse((string) rc[2], CultureInfo.InvariantCulture),
                    LowPrice = decimal.Parse((string) rc[3], CultureInfo.InvariantCulture),
                    Volume = decimal.Parse((string) rc[5], CultureInfo.InvariantCulture),
                    NumberOfTrades = (long) rc[8]
                }).ToList();

            return candles;
        }

        public static string ToIntervalString(this CandlesInterval interval)
        {
            switch (interval)
            {
                case CandlesInterval.Minutes1:
                    return "1m";
                case CandlesInterval.Minutes3:
                    return "3m";
                case CandlesInterval.Minutes5:
                    return "5m";
                case CandlesInterval.Minutes15:
                    return "15m";
                case CandlesInterval.Minutes30:
                    return "30m";
                case CandlesInterval.Hours1:
                    return "1h";
                default:
                    throw new ArgumentOutOfRangeException(nameof(interval), interval, null);
            }
        }

      
    }
}