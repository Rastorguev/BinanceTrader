using System;
using System.Collections.Generic;
using System.Linq;
using Binance.API.Csharp.Client.Models;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.TradeSessions
{
    public class RabbitTradeSession : ITradeSession
    {
        [NotNull] private readonly TradeSessionConfig _config;

        public RabbitTradeSession([NotNull] TradeSessionConfig config)
        {
            _config = config;
        }

        public ITradeAccount Run(List<Candlestick> candles)
        {
            var account = new MockTradeAccount(0, _config.InitialQuoteAmount, _config.InitialPrice, _config.Fee);

            if (!candles.Any())
            {
                return account;
            }

            var nextPrice = candles.First().NotNull().Close;
            var nextAction = OrderSide.Buy;
            DateTime? lastActionDate = null;

            foreach (var candle in candles)
            {
                var force = lastActionDate == null ||
                            candle.CloseTime.GetTime() - lastActionDate.Value >=
                            TimeSpan.FromHours(_config.MaxIdleHours);

                var minProfitRatio = _config.MinProfitRatio;
                var inRange = nextPrice >= candle.Low && nextPrice <= candle.High;

                if (nextAction == OrderSide.Buy && (inRange || force))
                {
                    var price = nextPrice;

                    if (force)
                    {
                        price = candle.High;
                    }

                    var estimatedBaseAmount = Math.Floor(account.CurrentQuoteAmount / price);
                    if (account.CurrentQuoteAmount > _config.MinQuoteAmount && estimatedBaseAmount > 0)
                    {
                        account.Buy(estimatedBaseAmount, price, candle.OpenTime.GetTime());

                        nextPrice = price + price.Percents(minProfitRatio);
                        nextAction = OrderSide.Sell;
                        lastActionDate = candle.OpenTime.GetTime();
                    }
                }
                else if (nextAction == OrderSide.Sell && (inRange || force))
                {
                    var price = nextPrice;

                    if (force)
                    {
                        price = candle.Low;
                    }

                    var baseAmount = Math.Floor(account.CurrentBaseAmount);
                    if (baseAmount > 0)
                    {
                        account.Sell(baseAmount, price, candle.OpenTime.GetTime());

                        nextPrice = price - price.Percents(minProfitRatio);
                        nextAction = OrderSide.Buy;
                        lastActionDate = candle.OpenTime.GetTime();
                    }
                }
            }

            return account;
        }
    }
}