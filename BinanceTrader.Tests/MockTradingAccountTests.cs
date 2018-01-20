using System;
using BinanceTrader.Core;
using Xunit;

namespace BinanceTrader.Tests
{
    public class MockTradingAccountTests
    {
        [Fact]
        public void Buy_InsufficientBalance_Exception()
        {
            var tradingAccount = new MockTradeAccount(
                initialBaseAmount: 100,
                initialQuoteAmount: 0,
                initialPrice: 0,
                fee: 1);

            Assert.Throws<InsufficientBalanceException>(
                () => { tradingAccount.Buy(100, 1, DateTime.Now); });
        }

        [Fact]
        public void BuyOnce_StateIsCorrect()
        {
            var tradingAccount = new MockTradeAccount(
                initialBaseAmount: 0,
                initialQuoteAmount: 1,
                initialPrice: 0,
                fee: 1);

            tradingAccount.Buy(50, 0.02m, DateTime.Now);

            Assert.Equal(0, tradingAccount.CurrentQuoteAmount);
            Assert.Equal(49.5m, tradingAccount.CurrentBaseAmount);
            Assert.Equal(0.02m, tradingAccount.LastPrice);
        }

        [Fact]
        public void BuyTwice_StateIsCorrect()
        {
            var tradingAccount = new MockTradeAccount(
                initialBaseAmount: 0,
                initialQuoteAmount: 1,
                initialPrice: 0,
                fee: 1);

            tradingAccount.Buy(10, 0.02m, DateTime.Now);
            tradingAccount.Buy(20, 0.01m, DateTime.Now);

            Assert.Equal(0.6m, tradingAccount.CurrentQuoteAmount);
            Assert.Equal(29.7m, tradingAccount.CurrentBaseAmount);
            Assert.Equal(0.01m, tradingAccount.LastPrice);
        }

        [Fact]
        public void Sell_InsufficientBalance_Exception()
        {
            var tradingAccount = new MockTradeAccount(
                initialBaseAmount: 0,
                initialQuoteAmount: 1,
                initialPrice: 0.01m,
                fee: 1);

            Assert.Throws<InsufficientBalanceException>(
                () => { tradingAccount.Sell(100, 1, DateTime.Now); });
        }

        [Fact]
        public void SellOnce_StateIsCorrect()
        {
            var tradingAccount = new MockTradeAccount(
                initialBaseAmount: 100,
                initialQuoteAmount: 0,
                initialPrice: 0.01m,
                fee: 1);

            tradingAccount.Sell(50, 0.01m, DateTime.Now);

            Assert.Equal(0.495m, tradingAccount.CurrentQuoteAmount);
            Assert.Equal(50, tradingAccount.CurrentBaseAmount);
            Assert.Equal(0.01m, tradingAccount.LastPrice);
        }

        [Fact]
        public void SellTwice_StateIsCorrect()
        {
            var tradingAccount = new MockTradeAccount(
                initialBaseAmount: 100,
                initialQuoteAmount: 0,
                initialPrice: 0.03m,
                fee: 1);

            tradingAccount.Sell(50, 0.01m, DateTime.Now);
            tradingAccount.Sell(10, 0.03m, DateTime.Now);

            Assert.Equal(0.495m + 0.297m, tradingAccount.CurrentQuoteAmount);
            Assert.Equal(40, tradingAccount.CurrentBaseAmount);
            Assert.Equal(0.03m, tradingAccount.LastPrice);
        }

        [Fact]
        public void BuyAndSell_StateIsCorrect()
        {
            var tradingAccount = new MockTradeAccount(
                initialBaseAmount: 0,
                initialQuoteAmount: 1,
                initialPrice: 0,
                fee: 1);

            tradingAccount.Buy(50, 0.01m, DateTime.Now);
            tradingAccount.Sell(40, 0.02m, DateTime.Now);

            Assert.Equal(1.292m, tradingAccount.CurrentQuoteAmount);
            Assert.Equal(9.5m, tradingAccount.CurrentBaseAmount);
            Assert.Equal(0.02m, tradingAccount.LastPrice);
        }
    }
}