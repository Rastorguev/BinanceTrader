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
            const int count = 200;
            var account = new MockTradeAccount(0, _config.InitialQuoteAmount, _config.InitialPrice, _config.Fee);
            var nextAction = TradeAction.Buy;
            DateTime? lastActionDate = null;

            if (!candles.Any())
            {
                return account;
            }

            for (var i = 0; i < candles.Count; i++)
            {
                var candle = candles[i].NotNull();
                var force = lastActionDate == null ||
                            candle.CloseTime.GetTime() - lastActionDate.Value >=
                            TimeSpan.FromHours(_config.MaxIdleHours);

                if (i < count)
                {
                    continue;
                }

                var price = candle.Close;
                var time = candle.OpenTime.GetTime();
                var range = candles.GetRange(i - count, count);

                var action = _strategy.GetTradeAction(range);

                if (nextAction == TradeAction.Buy && (action == TradeAction.Buy || force))
                {
                    if (force)
                    {
                        price = candle.High;
                    }

                    var baseAmount = Math.Floor(account.CurrentQuoteAmount / price);

                    if (account.CurrentQuoteAmount > _config.MinQuoteAmount && baseAmount > 0)
                    {
                        account.Buy(baseAmount, price, time);
                        nextAction = TradeAction.Sell;
                        lastActionDate = candle.OpenTime.GetTime();
                    }
                }
                else if (nextAction == TradeAction.Sell && action == TradeAction.Sell)
                {
                    var baseAmount = Math.Floor(account.CurrentBaseAmount);

                    if (baseAmount > 0 &&
                        price > account.LastPrice + account.LastPrice.Percents(_config.MinProfitRatio) || force)
                    {
                        if (force)
                        {
                            price = candle.Low;
                        }

                        account.Sell(baseAmount, price, time);
                        nextAction = TradeAction.Buy;
                        lastActionDate = candle.OpenTime.GetTime();
                    }
                }
            }

            return account;
        }
    }
}