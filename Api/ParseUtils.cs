using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BinanceTrader.Entities;
using BinanceTrader.Entities.Enums;

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

        public static Candles ToCandles(this List<List<object>> rawCandles)
        {
            var candles = new Candles();
            candles.AddRange(rawCandles.Select(
                arr => new Candle
                {
                    OpenTime = DateTimeOffset.FromUnixTimeMilliseconds((long) arr[0]).LocalDateTime,
                    CloseTime = DateTimeOffset.FromUnixTimeMilliseconds((long)arr[6]).LocalDateTime,
                    OpenPrice = decimal.Parse((string) arr[1], CultureInfo.InvariantCulture),
                    ClosePrice = decimal.Parse((string) arr[4], CultureInfo.InvariantCulture),
                    HighPrice = decimal.Parse((string) arr[2], CultureInfo.InvariantCulture),
                    LowPrice = decimal.Parse((string) arr[3], CultureInfo.InvariantCulture),
                    Volume = decimal.Parse((string) arr[5], CultureInfo.InvariantCulture),
                    NumberOfTrades = (long) arr[8]
                }));

            return candles;
        }
    }
}