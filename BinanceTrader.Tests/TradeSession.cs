using System;
using System.Collections.Generic;
using System.Linq;
using Binance.API.Csharp.Client.Models;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Strategies;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public class TradeSession
    {
        [NotNull] private readonly TradeSessionConfig _config;
        private OrderSide _nextAction = OrderSide.Buy;

        // ReSharper disable NotNullMemberIsNotInitialized
        public TradeSession([NotNull] TradeSessionConfig config) => _config = config;
        // ReSharper restore NotNullMemberIsNotInitialized

        [NotNull]
        public ITradeAccount Run([NotNull] [ItemNotNull] IReadOnlyList<Candlestick> candles,
            [NotNull] ITradingStrategy strategy)
        {
            var account = new MockTradeAccount(0, _config.InitialQuoteAmount, _config.InitialPrice, _config.Fee);

            if (!candles.Any())
            {
                return account;
            }

            for (var i = 0; i < candles.Count; i++)
            {
                var strategyAction = strategy.GetTradeAction(candles, i);
                var current = candles[i];

                if (_nextAction == OrderSide.Buy && strategyAction == TradeAction.Buy)
                {
                    Buy(account, current.Close, current);
                }

                if (_nextAction == OrderSide.Sell && strategyAction == TradeAction.Sell)
                {
                    Sell(account, current.Close, current);
                }
            }

            return account;
        }

        private void Buy([NotNull] ITradeAccount account, decimal price, [NotNull] Candlestick candle)
        {
            var estimatedBaseAmount = Math.Floor(account.CurrentQuoteAmount / price);
            if (account.CurrentQuoteAmount > _config.MinQuoteAmount && estimatedBaseAmount > 0)
            {
                account.Buy(estimatedBaseAmount, price, candle.OpenTime.GetTime());

                _nextAction = OrderSide.Sell;
            }
        }

        private void Sell([NotNull] ITradeAccount account, decimal price, [NotNull] Candlestick candle)
        {
            var baseAmount = Math.Floor(account.CurrentBaseAmount);
            if (baseAmount > 0)
            {
                account.Sell(baseAmount, price, candle.OpenTime.GetTime());

                _nextAction = OrderSide.Buy;
            }
        }
    }
}