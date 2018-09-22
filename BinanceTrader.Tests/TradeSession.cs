using System;
using System.Collections.Generic;
using System.Linq;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Extensions;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Tools;
using BinanceTrader.Trader;
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
            [NotNull] TradeSessionConfig config) => _config = config;

        [NotNull]
        public ITradeAccount Run([NotNull] [ItemNotNull] IReadOnlyList<Candlestick> candlesticks)
        {
            var candles = candlesticks.ToList();
            var account = new MockTradeAccount(0, _config.InitialQuoteAmount, _config.Fee);

            if (!candles.Any())
            {
                return account;
            }

            _nextPrice = candles.First().NotNull().Close.Round();
            _nextAction = OrderSide.Buy;
            _lastActionDate = null;

            foreach (var candle in candles)
            {
                var isExpired = _lastActionDate == null ||
                                candle.CloseTime.GetTime() - _lastActionDate.Value >=
                                _config.MaxIdlePeriod;

                //decimal GetProfitRatio() => CalculateProfitRatio(candles, candle, _config.MaxIdlePeriod);
                decimal GetProfitRatio()
                {
                    return _config.ProfitRatio;
                }

                var inRange = _nextPrice > candle.Low.Round() && _nextPrice < candle.High.Round();

                if (_nextAction == OrderSide.Buy)
                {
                    if (inRange)
                    {
                        var price = _nextPrice;
                        var nextPrice = (price + price.Percents(GetProfitRatio())).Round();

                        Buy(account, price, nextPrice, candle);
                        //Console.WriteLine($"Buy\t {price}\t {candle.OpenTime.GetTime().ToLocalTime()}");

                        _nextAction = OrderSide.Sell;
                        _lastActionDate = candle.OpenTime.GetTime();
                    }
                    else if (isExpired)
                    {
                        var price = candle.High.Round();
                        var nextPrice = (price - price.Percents(GetProfitRatio())).Round();

                        Cancel(account, nextPrice, candle);
                    }
                }
                else if (_nextAction == OrderSide.Sell)
                {
                    if (inRange)
                    {
                        var price = _nextPrice;
                        var nextPrice = (price - price.Percents(GetProfitRatio())).Round();

                        Sell(account, price, nextPrice, candle);
                        //Console.WriteLine($"Sell\t {price}\t {candle.OpenTime.GetTime().ToLocalTime()}");
                    }
                    else if (isExpired)
                    {
                        var price = candle.Low.Round();
                        var nextPrice = (price + price.Percents(GetProfitRatio())).Round();

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

        private decimal CalculateProfitRatio([NotNull] List<Candlestick> candles,
            [NotNull] Candlestick current, TimeSpan interval)
        {
           
            var defaultRatio = _config.ProfitRatio;
            var n = (int) interval.TotalMinutes;

            var endIndex = candles.IndexOf(current);
            var startIndex = Math.Max(0, endIndex - n);

            var range = candles.GetRange(startIndex, endIndex - startIndex);
            if (!range.Any())
            {
                return defaultRatio;
            }

            var volatility = TechAnalyzer.CalculateVolatility(range);
            var profitRatio = Math.Max(volatility, _config.Fee * 2);

            return profitRatio;
        }
    }
}