using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Tools;
using JetBrains.Annotations;
using Xunit;

namespace BinanceTrader
{
    public class QuoteDistributionTest
    {
        [Fact]
        public void OrdersDestributerTests()
        {
            var quoteAmount = 100;
            var orderSize = 10;
        }
    }

    public class OrdersDestributer
    {
        public Dictionary<string, List<decimal>> Distribute(
            decimal quoteAmount,
            decimal minOrderSize,
            [NotNull] Dictionary<string, int> openOrdersCount)
        {
            var ordersToPlace = new Dictionary<string, List<decimal>>();
            var maxOrders = openOrdersCount.Values.NotNull().Max();
            var remainingQuoteAmount = quoteAmount;

            var ordersCount = openOrdersCount.OrderBy(x => x.Value).ToList();
            foreach (var o in ordersCount)
            {
                var sumbol = o.Key;
                var ordersToPlaceCount = ordersToPlace.ContainsKey(sumbol) ? ordersToPlace[sumbol].NotNull().Count : 0;
                while (o.Value + ordersToPlaceCount <= maxOrders)
                {
                    if (remainingQuoteAmount < minOrderSize)
                    {
                        return ordersToPlace;
                    }

                    AddOrder(sumbol);
                }
            }

            while (true)
            {
                foreach (var o in ordersCount)
                {
                    var sumbol = o.Key;
                    if (remainingQuoteAmount < minOrderSize)
                    {
                        return ordersToPlace;
                    }

                    AddOrder(sumbol);
                }
            }

            void AddOrder(string sumbol)
            {
                if (!ordersToPlace.ContainsKey(sumbol))
                {
                    ordersToPlace[sumbol] = new List<decimal>();
                }

                var orderSize = remainingQuoteAmount >= minOrderSize * 2 ? minOrderSize : remainingQuoteAmount;
                ordersToPlace[sumbol].NotNull().Add(orderSize);

                remainingQuoteAmount -= orderSize;
            }
        }
    }
}