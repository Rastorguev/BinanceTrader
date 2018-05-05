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
            [NotNull] Dictionary<string, int> openOrdersCount)
        {
            var ordersRequests = new Dictionary<string, List<decimal>>();
            var remainingQuoteAmount = freeQuoteAmount;

            while (true)
            {
                var minOrdersCountSymbols = openOrdersCount.Select(o =>
                    {
                        var requestsCount = ordersRequests.ContainsKey(o.Key.NotNull())
                            ? ordersRequests[o.Key.NotNull()].NotNull().Count
                            : 0;

                        return (Symbol: o.Key, Count: requestsCount + o.Value);
                    })
                    .GroupBy(x => x.Count)
                    .OrderBy(g => g.Key)
                    .First().NotNull()
                    .Select(g => g.Symbol)
                    .ToList();

                foreach (var symbol in minOrdersCountSymbols)
                {
                    if (remainingQuoteAmount < minOrderSize)
                    {
                        return ordersRequests;
                    }

                    if (!ordersRequests.ContainsKey(symbol.NotNull()))
                    {
                        ordersRequests[symbol] = new List<decimal>();
                    }

                    var orderSize = remainingQuoteAmount >= minOrderSize * 2 ? minOrderSize : remainingQuoteAmount;
                    ordersRequests[symbol].NotNull().Add(orderSize);
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
            var ordersRequests = new List<decimal>();
            var remainingStepsAmount = (int) (freeBaseAmount / stepSize);

            var minOrderSizeInSteps = (int) (minOrderSize / stepSize / price);
            while (remainingStepsAmount >= minOrderSizeInSteps)
            {
                var orderSizeInSteps = remainingStepsAmount >= minOrderSizeInSteps * 2
                    ? minOrderSizeInSteps
                    : remainingStepsAmount;

                ordersRequests.Add(orderSizeInSteps * stepSize);
                remainingStepsAmount -= orderSizeInSteps;
            }

            return ordersRequests;
        }

        public static decimal GetFittingBaseAmount(decimal quoteAmount, decimal price, decimal stepSize)
        {
            return (int) (quoteAmount / price / stepSize) * stepSize;
        }
    }
}