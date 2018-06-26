using System;
using System.Collections.Generic;
using System.Linq;
using Binance.API.Csharp.Client.Models;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public class TradeSession
    {
        [NotNull] private readonly TradeSessionConfig _config;
        private decimal _nextPrice;
        private OrderSide _nextAction;
        private DateTime? _lastActionDate;

        public TradeSession(
            [NotNull] TradeSessionConfig config)
        {
            _config = config;
        }

        [NotNull]
        public ITradeAccount Run([NotNull] [ItemNotNull] IReadOnlyList<Candlestick> candles)
        {
            var account = new MockTradeAccount(0, _config.InitialQuoteAmount, _config.Fee);

            if (!candles.Any())
            {
                return account;
            }

            _nextPrice = candles.First().NotNull().Close;
            _nextAction = OrderSide.Buy;
            _lastActionDate = null;

            foreach (var candle in candles)
            {
                var isExpired = _lastActionDate == null ||
                            candle.CloseTime.GetTime() - _lastActionDate.Value >=
                            TimeSpan.FromHours((double) _config.MaxIdleHours);

                //decimal ProfitRatio() => CalculateProfitRatio(candles, candle, TimeSpan.FromHours((double)_config.MaxIdleHours));
                decimal ProfitRatio() => _config.ProfitRatio;
                var inRange = _nextPrice >= candle.Low && _nextPrice <= candle.High;

                if (_nextAction == OrderSide.Buy)
                {
                    if (inRange)
                    {
                        var price = _nextPrice;

                        Buy(account, price, candle);

                        _nextPrice = price + price.Percents(ProfitRatio());
                        _nextAction = OrderSide.Sell;
                        _lastActionDate = candle.OpenTime.GetTime();
                    }
                    else if (isExpired)
                    {
                        var price = candle.High;

                        _nextPrice = price - price.Percents(ProfitRatio());
                        account.IncreseCanceledCount();
                    }
                }
                else if (_nextAction == OrderSide.Sell)
                {
                    if (inRange)
                    {
                        var price = _nextPrice;

                        Sell(account, price, candle);

                        _nextPrice = price - price.Percents(ProfitRatio());
                        _nextAction = OrderSide.Buy;
                        _lastActionDate = candle.OpenTime.GetTime();
                    }
                    else if (isExpired)
                    {
                        var price = candle.Low;

                        _nextPrice = price + price.Percents(ProfitRatio());
                        account.IncreseCanceledCount();
                    }
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
            }
        }

        private void Sell([NotNull] ITradeAccount account, decimal price, [NotNull] Candlestick candle)
        {
            var baseAmount = Math.Floor(account.CurrentBaseAmount);
            if (baseAmount > 0)
            {
                account.Sell(baseAmount, price, candle.OpenTime.GetTime());
            }
        }

        private decimal CalculateProfitRatio([NotNull] IReadOnlyList<Candlestick> candles,
            [NotNull] Candlestick current, TimeSpan interval)
        {
            var list = candles.ToList();
            var defaultRatio = 1;
            var start = list.FirstOrDefault(c => c.OpenTime.GetTime() == current.OpenTime.GetTime() - interval);

            if (start == null)
            {
                return defaultRatio;
            }

            var startIndex = list.IndexOf(start);
            var endIndex = list.IndexOf(current);
            var range = candles.ToList().GetRange(startIndex, endIndex - startIndex);

            var min = range.Min(c => c.NotNull().Low);
            var max = range.Max(c => c.NotNull().High);
            var gain = MathUtils.Gain(min, max);
            var adjustedGain = gain / (decimal) interval.TotalHours / 2;

            if (adjustedGain < defaultRatio)
            {
                return defaultRatio;
            }

            return adjustedGain;
        }
    }
}