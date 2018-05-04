using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Tools;
using BinanceTrader.Trader;
using Xunit;

namespace BinanceTrader
{
    public class QuoteDistributionTest
    {
        [Fact]
        public void DistributeQuoteCurrency_AllSymbolsHaveNoOpenOrders()
        {
            const int quoteAmount = 100;
            const int minOrderSize = 10;
            var ordersRequest = OrdersDistributor.DistributeQuoteCurrency(quoteAmount, minOrderSize,
                new Dictionary<string, int>
                {
                    {"0", 0},
                    {"1", 0},
                    {"2", 0},
                    {"3", 0},
                    {"4", 0},
                    {"5", 0},
                    {"6", 0},
                    {"7", 0},
                    {"8", 0},
                    {"9", 0}
                });

            Assert.True(ordersRequest.All(r => r.Value.NotNull().ToList().Count == 1));
            Assert.True(ordersRequest.Sum(r => r.Value.NotNull().Sum()) == quoteAmount);
        }

        [Fact]
        public void DistributeQuoteCurrency_OneSymbolHaveOneOpenOrder()
        {
            const int quoteAmount = 100;
            const int minOrderSize = 10;

            var ordersRequest = OrdersDistributor.DistributeQuoteCurrency(quoteAmount, minOrderSize,
                new Dictionary<string, int>
                {
                    {"0", 1},
                    {"1", 0},
                    {"2", 0},
                    {"3", 0},
                    {"4", 0},
                    {"5", 0},
                    {"6", 0},
                    {"7", 0},
                    {"8", 0},
                    {"9", 0}
                });

            Assert.True(ordersRequest.Sum(r => r.Value.Sum()) == quoteAmount);
            Assert.True(ordersRequest.All(r => r.Value.ToList().Count == 1));
        }

        [Fact]
        public void DistributeQuoteCurrency_OneSymbolHaveManyOpenOrder()
        {
            const int quoteAmount = 100;
            const int minOrderSize = 10;

            var ordersRequest = OrdersDistributor.DistributeQuoteCurrency(quoteAmount, minOrderSize,
                new Dictionary<string, int>
                {
                    {"0", 25},
                    {"1", 0},
                    {"2", 0},
                    {"3", 0},
                    {"4", 0},
                    {"5", 0},
                    {"6", 0},
                    {"7", 0},
                    {"8", 0},
                    {"9", 0}
                });

            Assert.True(ordersRequest.Sum(r => r.Value.Sum()) == quoteAmount);
            Assert.True(ordersRequest.Where(o => o.Key != "1").All(r => r.Value.ToList().Count == 1));
            Assert.True(ordersRequest.First(o => o.Key == "1").Value.NotNull().Count == 2);
        }

        [Fact]
        public void DistributeQuoteCurrency_AllSymbolsHaveDifferentNumberOfOpenOrders()
        {
            const int quoteAmount = 1000;
            const int minOrderSize = 10;

            var ordersRequest = OrdersDistributor.DistributeQuoteCurrency(quoteAmount, minOrderSize,
                new Dictionary<string, int>
                {
                    {"0", 0},
                    {"1", 1},
                    {"2", 2},
                    {"3", 3},
                    {"4", 4},
                    {"5", 5},
                    {"6", 6},
                    {"7", 7},
                    {"8", 8},
                    {"9", 9}
                });

            Assert.True(ordersRequest.Sum(r => r.Value.Sum()) == quoteAmount);
            Assert.True(ordersRequest.First(o => o.Key == "0").Value.NotNull().Count == 15);
            Assert.True(ordersRequest.First(o => o.Key == "1").Value.NotNull().Count == 14);
            Assert.True(ordersRequest.First(o => o.Key == "2").Value.NotNull().Count == 13);
            Assert.True(ordersRequest.First(o => o.Key == "3").Value.NotNull().Count == 12);
            Assert.True(ordersRequest.First(o => o.Key == "4").Value.NotNull().Count == 11);
            Assert.True(ordersRequest.First(o => o.Key == "5").Value.NotNull().Count == 9);
            Assert.True(ordersRequest.First(o => o.Key == "6").Value.NotNull().Count == 8);
            Assert.True(ordersRequest.First(o => o.Key == "7").Value.NotNull().Count == 7);
            Assert.True(ordersRequest.First(o => o.Key == "8").Value.NotNull().Count == 6);
            Assert.True(ordersRequest.First(o => o.Key == "9").Value.NotNull().Count == 5);
        }

        [Fact]
        public void DistributeQuoteCurrency_QuoteAmountNotDivisibleByMinOrderSize()
        {
            const decimal quoteAmount = 1005.25m;
            const int minOrderSize = 10;

            var ordersRequest = OrdersDistributor.DistributeQuoteCurrency(quoteAmount, minOrderSize,
                new Dictionary<string, int>
                {
                    {"0", 0},
                    {"1", 1},
                    {"2", 2},
                    {"3", 3},
                    {"4", 4},
                    {"5", 5},
                    {"6", 6},
                    {"7", 7},
                    {"8", 8},
                    {"9", 9}
                });

            Assert.True(ordersRequest.Sum(r => r.Value.Sum()) == quoteAmount);
            Assert.True(ordersRequest.First(o => o.Key == "0").Value.NotNull().Count == 15);
            Assert.True(ordersRequest.First(o => o.Key == "1").Value.NotNull().Count == 14);
            Assert.True(ordersRequest.First(o => o.Key == "2").Value.NotNull().Count == 13);
            Assert.True(ordersRequest.First(o => o.Key == "3").Value.NotNull().Count == 12);
            Assert.True(ordersRequest.First(o => o.Key == "4").Value.NotNull().Count == 11);
            Assert.True(ordersRequest.First(o => o.Key == "5").Value.NotNull().Count == 9);
            Assert.True(ordersRequest.First(o => o.Key == "6").Value.NotNull().Count == 8);
            Assert.True(ordersRequest.First(o => o.Key == "7").Value.NotNull().Count == 7);
            Assert.True(ordersRequest.First(o => o.Key == "8").Value.NotNull().Count == 6);
            Assert.True(ordersRequest.First(o => o.Key == "9").Value.NotNull().Count == 5);
            Assert.True(ordersRequest.First(o => o.Key == "4").Value.NotNull().Last() == 15.25m);
        }

        [Fact]
        public void DistributeBaseCurrency_BaseAmountEqualsToOrderMinSize()
        {
            const decimal baseAmount = 1000;
            const int minOrderSize = 10;
            const decimal price = 0.01m;
            const decimal stepSize = 1;

            var ordersRequest = OrdersDistributor.DistributeBaseCurrency(baseAmount, minOrderSize, price, stepSize);

            Assert.Equal(baseAmount, ordersRequest.Sum(r => r));
            Assert.Single(ordersRequest);
            Assert.True(ordersRequest.First() == 1000);
        }

        [Fact]
        public void DistributeBaseCurrency_BaseAmountGreaterThanOrderMinSize()
        {
            const decimal baseAmount = 1050.18m;
            const int minOrderSize = 10;
            const decimal price = 0.01m;
            const decimal stepSize = 1;

            var ordersRequest = OrdersDistributor.DistributeBaseCurrency(baseAmount, minOrderSize, price, stepSize);

            Assert.Equal(baseAmount, ordersRequest.Sum(r => r) + 0.18m);
            Assert.Single(ordersRequest);
            Assert.Equal(1050, ordersRequest.First());
        }

        [Fact]
        public void DistributeBaseCurrency_BasePriceIsGreaterThanQuotePrice()
        {
            const decimal baseAmount = 10.013m;
            const int minOrderSize = 10;
            const decimal price = 10m;
            const decimal stepSize = 0.01m;

            var ordersRequest = OrdersDistributor.DistributeBaseCurrency(baseAmount, minOrderSize, price, stepSize);

            Assert.Equal(baseAmount, ordersRequest.Sum(r => r) + 0.003m);
            Assert.Equal(10, ordersRequest.Count);
            Assert.Equal(1.01m, ordersRequest.Last());
        }
    }
}