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
            [NotNull] Dictionary<string, decimal> priceGain
        )
        {
            var amounts = new Dictionary<string, List<decimal>>();
            var remainingQuoteAmount = freeQuoteAmount;
            var positiveGainSymbols = priceGain.Where(g => g.Value > 0m).ToList();

            if (!positiveGainSymbols.Any())
            {
                return amounts;
            }

            var topGainSymbols = positiveGainSymbols
                .OrderByDescending(g => g.Value)
                .Take(Math.Min(positiveGainSymbols.Count, 5))
                .Select(g => g.Key).ToList();

            while (remainingQuoteAmount >= minOrderSize)
            {
                foreach (var symbol in topGainSymbols)
                {
                    if (!amounts.ContainsKey(symbol.NotNull()))
                    {
                        amounts[symbol] = new List<decimal>();
                    }

                    var orderSize = remainingQuoteAmount >= minOrderSize * 2 ? minOrderSize : remainingQuoteAmount;
                    amounts[symbol].NotNull().Add(orderSize);
                    remainingQuoteAmount -= orderSize;
                }
            }

            return amounts;
        }

        [NotNull]
        public static List<decimal> SplitIntoSellOrders(
            decimal freeBaseAmount,
            decimal minOrderSize,
            decimal price,
            decimal stepSize)
        {
            var amounts = new List<decimal>();
            var remainingStepsAmount = (int) (freeBaseAmount / stepSize);

            var minOrderSizeInSteps = (int) (minOrderSize / stepSize / price);
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
            return (int) (quoteAmount / price / stepSize) * stepSize;
        }
    }
}