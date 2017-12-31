using System;
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
    }
}