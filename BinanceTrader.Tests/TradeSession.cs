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
                                _config.MaxIdlePeriod;

                //decimal ProfitRatio() => CalculateProfitRatio(candles, candle, TimeSpan.FromHours((double)_config.MaxIdlePeriod));
                decimal ProfitRatio() => _config.ProfitRatio;
                var inRange = _nextPrice >= candle.Low && _nextPrice <= candle.High;

                if (_nextAction == OrderSide.Buy)
                {
                    if (inRange)
                    {
                        var price = _nextPrice;
                        var nextPrice = price + price.Percents(ProfitRatio());

                        Buy(account, price, nextPrice, candle);

                        _nextAction = OrderSide.Sell;
                        _lastActionDate = candle.OpenTime.GetTime();
                    }
                    else if (isExpired)
                    {
                        var price = candle.High;
                        var nextPrice = price - price.Percents(ProfitRatio());

                        Cancel(account, nextPrice, candle);
                    }
                }
                else if (_nextAction == OrderSide.Sell)
                {
                    if (inRange)
                    {
                        var price = _nextPrice;
                        var nextPrice = price - price.Percents(ProfitRatio());

                        Sell(account, price, nextPrice, candle);
                    }
                    else if (isExpired)
                    {
                        var price = candle.Low;
                        var nextPrice = price + price.Percents(ProfitRatio());

                        Cancel(account, nextPrice, candle);
                    }
                }
            }

            return account;
        }

        private void Buy([NotNull] ITradeAccount account, decimal price, decimal nextPrice,
            [NotNull] Candlestick candle)
        {
            var estimatedBaseAmount = Math.Floor(account.CurrentQuoteAmount / price);
            if (account.CurrentQuoteAmount > _config.MinQuoteAmount && estimatedBaseAmount > 0)
            {
                account.Buy(estimatedBaseAmount, price, candle.OpenTime.GetTime());

                _nextPrice = nextPrice;
                _nextAction = OrderSide.Buy;
                _lastActionDate = candle.OpenTime.GetTime();
            }
        }

        private void Sell([NotNull] ITradeAccount account, decimal price, decimal nextPrice,
            [NotNull] Candlestick candle)
        {
            var baseAmount = Math.Floor(account.CurrentBaseAmount);
            if (baseAmount > 0)
            {
                account.Sell(baseAmount, price, candle.OpenTime.GetTime());

                _nextPrice = nextPrice;
                _nextAction = OrderSide.Buy;
                _lastActionDate = candle.OpenTime.GetTime();
            }
        }

        private void Cancel([NotNull] ITradeAccount account, decimal nextPrice, [NotNull] Candlestick candle)
        {
            _nextPrice = nextPrice;
            _lastActionDate = candle.OpenTime.GetTime();
            account.IncreaseCanceledCount();
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