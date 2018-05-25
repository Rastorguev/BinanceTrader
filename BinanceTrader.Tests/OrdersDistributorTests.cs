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
        public void SplitIntoBuyOrders_AllSymbolsHaveNoOpenOrdersEqualFluctuations()
        {
            const int quoteAmount = 100;
            const int minOrderSize = 10;

            var openOrders = new Dictionary<string, int>
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
            };

            var flucs = new Dictionary<string, decimal>
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
            };

            var amounts = OrderDistributor.SplitIntoBuyOrders(quoteAmount, minOrderSize, openOrders, flucs);

            Assert.True(amounts.All(r => r.Value.NotNull().ToList().Count == 1));
            Assert.True(amounts.Sum(r => r.Value.NotNull().Sum()) == quoteAmount);
        }

        [Fact]
        public void SplitIntoBuyOrders_AllSymbolsHaveNoOpenOrderDifferentFluctuations()
        {
            const int quoteAmount = 100;
            const int minOrderSize = 10;

            var openOrders = new Dictionary<string, int>
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
            };

            var flucts = new Dictionary<string, decimal>
            {
                {"0", 0},
                {"1", 1},
                {"2", 2},
                {"3", 3},
                {"4", 0},
                {"5", 0},
                {"6", 0},
                {"7", 0},
                {"8", 0},
                {"9", 0}
            };

            var amounts = OrderDistributor.SplitIntoBuyOrders(quoteAmount, minOrderSize, openOrders, flucts);
            Assert.True(amounts.First(o => o.Key == "1").Value.NotNull().Count == 2);
            Assert.True(amounts.First(o => o.Key == "2").Value.NotNull().Count == 3);
            Assert.True(amounts.First(o => o.Key == "3").Value.NotNull().Count == 5);

            Assert.True(amounts.Sum(r => r.Value.NotNull().Sum()) == quoteAmount);
        }

        [Fact]
        public void SplitIntoBuyOrders_OneSymbolHaveOneOpenOrder()
        {
            const int quoteAmount = 100;
            const int minOrderSize = 10;

            var openOrders = new Dictionary<string, int>
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
            };

            var flucts = new Dictionary<string, decimal>
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
            };

            var amounts = OrderDistributor.SplitIntoBuyOrders(quoteAmount, minOrderSize, openOrders, flucts);

            Assert.True(amounts.Sum(r => r.Value.NotNull().Sum()) == quoteAmount);
            Assert.True(amounts.All(r => r.Value.NotNull().ToList().Count == 1));
        }

        [Fact]
        public void SplitIntoBuyOrders_OneSymbolHaveManyOpenOrder()
        {
            const int quoteAmount = 100;
            const int minOrderSize = 10;

            var openOrders = new Dictionary<string, int>
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
            };

            var flucts = new Dictionary<string, decimal>
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
            };

            var amounts = OrderDistributor.SplitIntoBuyOrders(quoteAmount, minOrderSize, openOrders, flucts);

            Assert.True(amounts.Sum(r => r.Value.NotNull().Sum()) == quoteAmount);
            Assert.True(amounts.Where(o => o.Key != "1").All(r => r.Value.NotNull().ToList().Count == 1));
            Assert.True(amounts.First(o => o.Key == "1").Value.NotNull().Count == 2);
        }

        [Fact]
        public void SplitIntoBuyOrders_AllSymbolsHaveDifferentNumberOfOpenOrdersDifferentFluctuations()
        {
            const int quoteAmount = 1000;
            const int minOrderSize = 10;

            var openOrders = new Dictionary<string, int>
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
            };

            var flucts = new Dictionary<string, decimal>
            {
                {"0", 9},
                {"1", 8},
                {"2", 7},
                {"3", 6},
                {"4", 5},
                {"5", 4},
                {"6", 3},
                {"7", 2},
                {"8", 1},
                {"9", 0}
            };

            var amounts = OrderDistributor.SplitIntoBuyOrders(quoteAmount, minOrderSize, openOrders, flucts);

            Assert.True(amounts.Sum(r => r.Value.NotNull().Sum()) == quoteAmount);
            Assert.True(amounts.First(o => o.Key == "0").Value.NotNull().Count == 27);
            Assert.True(amounts.First(o => o.Key == "1").Value.NotNull().Count == 23);
            Assert.True(amounts.First(o => o.Key == "2").Value.NotNull().Count == 18);
            Assert.True(amounts.First(o => o.Key == "3").Value.NotNull().Count == 14);
            Assert.True(amounts.First(o => o.Key == "4").Value.NotNull().Count == 10);
            Assert.True(amounts.First(o => o.Key == "5").Value.NotNull().Count == 6);
            Assert.True(amounts.First(o => o.Key == "6").Value.NotNull().Count == 2);
        }

        [Fact]
        public void SplitIntoBuyOrders_QuoteAmountNotDivisibleByMinOrderSize()
        {
            const decimal quoteAmount = 1005.25m;
            const int minOrderSize = 10;

            var openOrders = new Dictionary<string, int>
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
            };

            var flucts = new Dictionary<string, decimal>
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
            };

            var amounts = OrderDistributor.SplitIntoBuyOrders(quoteAmount, minOrderSize, openOrders, flucts);

            Assert.True(amounts.Sum(r => r.Value.NotNull().Sum()) == quoteAmount);
            Assert.True(amounts.First(o => o.Key == "0").Value.NotNull().Count == 15);
            Assert.True(amounts.First(o => o.Key == "1").Value.NotNull().Count == 14);
            Assert.True(amounts.First(o => o.Key == "2").Value.NotNull().Count == 13);
            Assert.True(amounts.First(o => o.Key == "3").Value.NotNull().Count == 12);
            Assert.True(amounts.First(o => o.Key == "4").Value.NotNull().Count == 11);
            Assert.True(amounts.First(o => o.Key == "5").Value.NotNull().Count == 9);
            Assert.True(amounts.First(o => o.Key == "6").Value.NotNull().Count == 8);
            Assert.True(amounts.First(o => o.Key == "7").Value.NotNull().Count == 7);
            Assert.True(amounts.First(o => o.Key == "8").Value.NotNull().Count == 6);
            Assert.True(amounts.First(o => o.Key == "9").Value.NotNull().Count == 5);
            Assert.True(amounts.First(o => o.Key == "4").Value.NotNull().Last() == 15.25m);
        }

        [Fact]
        public void SplitIntoSellOrders_BaseAmountEqualsToOrderMinSize()
        {
            const decimal baseAmount = 1000;
            const int minOrderSize = 10;
            const decimal price = 0.01m;
            const decimal stepSize = 1;

            var amounts = OrderDistributor.SplitIntoSellOrders(baseAmount, minOrderSize, price, stepSize);

            Assert.Equal(baseAmount, amounts.Sum(r => r));
            Assert.Single(amounts);
            Assert.True(amounts.First() == 1000);
        }

        [Fact]
        public void SplitIntoSellOrders_BaseAmountGreaterThanOrderMinSize()
        {
            const decimal baseAmount = 1050.18m;
            const int minOrderSize = 10;
            const decimal price = 0.01m;
            const decimal stepSize = 1;

            var amounts = OrderDistributor.SplitIntoSellOrders(baseAmount, minOrderSize, price, stepSize);

            Assert.Equal(baseAmount, amounts.Sum(r => r) + 0.18m);
            Assert.Single(amounts);
            Assert.Equal(1050, amounts.First());
        }

        [Fact]
        public void SplitIntoSellOrders_BasePriceIsGreaterThanQuotePrice()
        {
            const decimal baseAmount = 10.013m;
            const int minOrderSize = 10;
            const decimal price = 10m;
            const decimal stepSize = 0.01m;

            var amounts = OrderDistributor.SplitIntoSellOrders(baseAmount, minOrderSize, price, stepSize);

            Assert.Equal(baseAmount, amounts.Sum(r => r) + 0.003m);
            Assert.Equal(10, amounts.Count);
            Assert.Equal(1.01m, amounts.Last());
        }
    }
}