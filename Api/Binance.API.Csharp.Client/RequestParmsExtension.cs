using System;
using Binance.API.Csharp.Client.Models.Enums;

namespace Binance.API.Csharp.Client
{
    public static class RequestParmsExtension
    {
        public static string ToRequestParam(this OrderSide side)
        {
            switch (side)
            {
                case OrderSide.Buy:
                    return "BUY";
                case OrderSide.Sell:
                    return "SELL";
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        public static string ToRequestParam(this OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.Limit:
                    return "LIMIT";
                case OrderType.Market:
                    return "MARKET";
                case OrderType.StopLoss:
                    return "STOP_LOSS";
                case OrderType.StopLossLimit:
                    return "STOP_LOSS_LIMIT";
                case OrderType.TakeProfit:
                    return "TAKE_PROFIT";
                case OrderType.TakeProfitLimit:
                    return "TAKE_PROFIT_LIMIT";
                case OrderType.LimitMaker:
                    return "LIMIT_MAKER";
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderType), orderType, null);
            }
        }
    }
}