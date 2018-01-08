using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BinanceTrader.Entities;
using BinanceTrader.Entities.Enums;
using JetBrains.Annotations;

namespace BinanceTrader.Api
{
    public static class ParseUtils
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

        public static CandlesChart ToCandles([NotNull] this List<List<object>> rawCandles)
        {
            var candles = new CandlesChart();
            candles.Candles.AddRange(rawCandles.Select(rc =>
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
                }));

            return candles;
        }
    }
}