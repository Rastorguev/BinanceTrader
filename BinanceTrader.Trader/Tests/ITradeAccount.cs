using System;
using System.Collections.Generic;
using Binance.API.Csharp.Client.Models.Enums;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Tests
{
    public interface ITradeAccount
    {
        decimal InitialBaseAmount { get; }
        decimal InitialQuoteAmount { get; }
        decimal InitialPrice { get; }
        decimal CurrentBaseAmount { get; }
        decimal CurrentQuoteAmount { get; }
        decimal LastPrice { get; }

        [NotNull]
        [ItemNotNull]
        IReadOnlyList<TradeLogItem> TradesLog { get; }

        void Buy(decimal baseAmount, decimal price, DateTime timestamp);
        void Sell(decimal baseAmount, decimal price, DateTime timestamp);
    }

    public static class TradeAccountExtensions
    {
        public static decimal GetProfit([NotNull] this ITradeAccount account)
        {
            var initialAmount = account.InitialBaseAmount * account.InitialPrice + account.InitialQuoteAmount;
            var currentAmount = account.CurrentBaseAmount * account.LastPrice + account.CurrentQuoteAmount;
            var profit = MathUtils.CalculateProfit(initialAmount, currentAmount).Round();

            return profit;
        }
    }

    public class MockTradeAccount : ITradeAccount
    {
        private readonly decimal _fee;
        [NotNull] [ItemNotNull] private readonly List<TradeLogItem> _log = new List<TradeLogItem>();

        public MockTradeAccount(decimal initialBaseAmount, decimal initialQuoteAmount, decimal initialPrice,
            decimal fee)
        {
            CurrentBaseAmount = InitialBaseAmount = initialBaseAmount;
            CurrentQuoteAmount = InitialQuoteAmount = initialQuoteAmount;
            LastPrice = InitialPrice = initialPrice;
            _fee = fee;
        }

        public decimal InitialBaseAmount { get; }
        public decimal InitialQuoteAmount { get; }
        public decimal InitialPrice { get; }
        public decimal CurrentBaseAmount { get; private set; }
        public decimal CurrentQuoteAmount { get; private set; }
        public decimal LastPrice { get; private set; }
        public IReadOnlyList<TradeLogItem> TradesLog => _log;

        public void Buy(decimal baseAmount, decimal price, DateTime timestamp)
        {
            var quoteAmount = baseAmount * price;
            if (quoteAmount > CurrentQuoteAmount)
            {
                throw new Exception("Insufficient Balance");
            }

            CurrentQuoteAmount -= quoteAmount;
            CurrentBaseAmount += baseAmount - baseAmount.Percents(_fee);
            LastPrice = price;

            Log(OrderSide.Buy, timestamp);
        }

        public void Sell(decimal baseAmount, decimal price, DateTime timestamp)
        {
            if (CurrentBaseAmount < baseAmount)
            {
                throw new Exception("Insufficient Balance");
            }

            var quoteAmount = baseAmount * price;
            CurrentBaseAmount -= baseAmount;
            CurrentQuoteAmount += quoteAmount - quoteAmount.Percents(_fee);
            LastPrice = price;

            Log(OrderSide.Sell, timestamp);
        }

        private void Log(OrderSide side, DateTime timestamp)
        {
            _log.Add(new TradeLogItem(
                timestamp,
                side,
                LastPrice,
                CurrentBaseAmount, CurrentQuoteAmount,
                this.GetProfit()));
        }
    }

    public class TradeLogItem
    {
        public DateTime Timestamp { get; }
        public OrderSide Side { get; }
        public decimal Price { get; }
        public decimal BaseAmount { get; }
        public decimal QuoteAmount { get; }
        public decimal Profit { get; }

        public TradeLogItem(
            DateTime timestamp,
            OrderSide side,
            decimal price,
            decimal baseAmount,
            decimal quoteAmount,
            decimal profit)
        {
            Timestamp = timestamp;
            Side = side;
            Price = price;
            BaseAmount = baseAmount;
            QuoteAmount = quoteAmount;
            Profit = profit;
        }
    }
}