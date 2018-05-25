using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public static class OrderDistributor
    {
        [NotNull]
        public static Dictionary<string, List<decimal>> SplitIntoBuyOrders(
            decimal freeQuoteAmount,
            decimal minOrderSize,
            [NotNull] Dictionary<string, int> openOrdersCount,
            [NotNull] Dictionary<string, decimal> priceFluctuations
        )
        {
            var amounts = new Dictionary<string, List<decimal>>();
            var remainingQuoteAmount = freeQuoteAmount;

            while (true)
            {
                var prioritySymbols = openOrdersCount.Select(
                        o =>
                        {
                            priceFluctuations.TryGetValue(o.Key.NotNull(), out var fluct);
                            var requestsCount = amounts.ContainsKey(o.Key.NotNull())
                                ? amounts[o.Key.NotNull()].NotNull().Count
                                : 0;

                            var openCount = o.Value;

                            if (fluct == 0m)
                            {
                                fluct = (decimal)Math.Pow(10, -10);
                            }

                            var priority = (requestsCount + openCount + 1) / fluct;

                            return (Symbol: o.Key, Priority: priority);
                        })
                    .GroupBy(x => x.Priority)
                    .OrderBy(g => g.Key)
                    .First().NotNull()
                    .Select(g => g.Symbol)
                    .ToList();

                foreach (var symbol in prioritySymbols)
                {
                    if (remainingQuoteAmount < minOrderSize)
                    {
                        return amounts;
                    }

                    if (!amounts.ContainsKey(symbol.NotNull()))
                    {
                        amounts[symbol] = new List<decimal>();
                    }

                    var orderSize = remainingQuoteAmount >= minOrderSize * 2 ? minOrderSize : remainingQuoteAmount;
                    amounts[symbol].NotNull().Add(orderSize);
                    remainingQuoteAmount -= orderSize;
                }
            }
        }

        [NotNull]
        public static List<decimal> SplitIntoSellOrders(
            decimal freeBaseAmount,
            decimal minOrderSize,
            decimal price,
            decimal stepSize)
        {
            var amounts = new List<decimal>();
            var remainingStepsAmount = (int)(freeBaseAmount / stepSize);

            var minOrderSizeInSteps = (int)(minOrderSize / stepSize / price);
            while (remainingStepsAmount >= minOrderSizeInSteps)
            {
                var orderSizeInSteps = remainingStepsAmount >= minOrderSizeInSteps * 2
                    ? minOrderSizeInSteps
                    : remainingStepsAmount;

                amounts.Add(orderSizeInSteps * stepSize);
                remainingStepsAmount -= orderSizeInSteps;
            }

            return amounts;
        }

        public static decimal GetFittingBaseAmount(decimal quoteAmount, decimal price, decimal stepSize)
        {
            return (int)(quoteAmount / price / stepSize) * stepSize;
        }
    }
}