﻿using System;
using Binance.API.Csharp.Client.Models.Market.TradingRules;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public static class RulesHelper
    {
        public static decimal GetMaxFittingPrice(decimal price, [NotNull] ITradingRules rules)
        {
            return (int)(price / rules.TickSize) * rules.TickSize;
        }

        public static decimal GetMaxFittingQty(decimal qty, [NotNull] ITradingRules rules)
        {
            return (int)(qty / rules.StepSize) * rules.StepSize;
        }

        public static decimal GetMinNotionalQty(decimal price, [NotNull] ITradingRules rules)
        {
            var qty = rules.MinNotional / price;

            return Math.Ceiling(qty / rules.StepSize) * rules.StepSize;
        }

        public static decimal GetFittingBaseAmount(decimal quoteAmount, decimal price, [NotNull] ITradingRules rules)
        {
            return (int)(quoteAmount / price / rules.StepSize) * rules.StepSize;
        }

        public static bool MeetsTradingRules([NotNull] this OrderRequest order, [NotNull] ITradingRules rules)
        {
            return
                order.Price >= rules.MinPrice || rules.MinPrice == 0 &&
                order.Price <= rules.MaxPrice || rules.MaxPrice == 0 &&
                order.Price * order.Qty >= rules.MinNotional &&
                (order.Price - rules.MinQty) % rules.TickSize == 0 &&
                order.Qty >= rules.MinQty &&
                order.Qty <= rules.MaxQty &&
                (order.Qty - rules.MinQty) % rules.StepSize == 0;
        }
    }
}