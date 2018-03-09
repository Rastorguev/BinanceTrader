using System;
using System.Collections.Generic;
using System.Linq;
using Binance.API.Csharp.Client.Models;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Strategies;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.TradeSessions
{
    public class StrategyTradeSession : ITradeSession
    {
        [NotNull] private readonly TradeSessionConfig _config;
        [NotNull] private readonly ITradeStrategy _strategy;

        public StrategyTradeSession([NotNull] TradeSessionConfig config, [NotNull] ITradeStrategy strategy)
        {
            _config = config;
            _strategy = strategy;
        }

        public ITradeAccount Run(List<Candlestick> candles)
        {
            const int max = 100;
            var account = new MockTradeAccount(0, _config.InitialQuoteAmount, _config.InitialPrice, _config.Fee);
            var nextAction = TradeAction.Buy;

            if (!candles.Any())
            {
                return account;
            }

            for (var i = 0; i < candles.Count; i++)
            {
                if (i == 0)
                {
                    continue;
                }

                var price = candles[i].NotNull().Close;
                var time = candles[i].NotNull().OpenTime.GetTime();

                var start = i < max ? 0 : i - max;
                var range = candles.GetRange(start, i - start + 1);

                if (range.Count > 200)
                {
                    range = range.GetRange(range.Count - 200 - 1, 200);
                }

                var action = _strategy.GetTradeAction(range);

                if (nextAction == TradeAction.Buy && action == TradeAction.Buy)
                {
                    var baseAmount = Math.Floor(account.CurrentQuoteAmount / price);

                    if (account.CurrentQuoteAmount > _config.MinQuoteAmount && baseAmount > 0)
                    {
                        account.Buy(baseAmount, price, time);
                        nextAction = TradeAction.Sell;
                    }
                }
                else if (nextAction == TradeAction.Sell && action == TradeAction.Sell)
                {
                    var baseAmount = Math.Floor(account.CurrentBaseAmount);

                    if (baseAmount > 0 &&
                        price > account.LastPrice + account.LastPrice.Percents(_config.MinProfitRatio))
                    {
                        account.Sell(baseAmount, price, time);
                        nextAction = TradeAction.Buy;
                    }
                }
            }

            return account;
        }
    }
}