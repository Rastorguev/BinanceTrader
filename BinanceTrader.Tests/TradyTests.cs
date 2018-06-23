using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.API.Csharp.Client.Models;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;
using Trady.Analysis;
using Trady.Analysis.Backtest;
using Trady.Analysis.Extension;

namespace BinanceTrader
{
    public class TradyTests
    {
        [NotNull] private readonly ICandlesProvider _candlesProvider;

        public TradyTests([NotNull] ICandlesProvider candlesProvider)
        {
            _candlesProvider = candlesProvider;
        }

        public async Task Execute()
        {
            var candlesticks = await _candlesProvider.GetCandles(
                    "BTC",
                    "USDT",
                    new DateTime(2018, 04, 1, 0, 0, 0),
                    new DateTime(2018, 06, 1, 0, 0, 0),
                    TimeInterval.Hours_1)
                .NotNull();

            var candles = candlesticks.ToIndexedToIOhlcvList();

            var buyRule = Rule
                .Create(c => c.IsFullStoOversold(20, 5, 5));

            var sellRule = Rule
                .Create(c => c.IsFullStoOversold(20, 5, 5));

            var runner = new Builder()
                .Add(candles).NotNull()
                .Buy(buyRule).NotNull()
                .Sell(sellRule).NotNull()
                .Build()
                .NotNull();

            var result = await runner.RunAsync(100, 0.05m).NotNull();
            var tr = result.NotNull().Transactions.NotNull().Select(t => t.NotNull().DateTime.ToLocalTime()).ToList();
            var s = "";
        }
    }

    public static class AnalyticsExtensions
    {
        public static T GetValueByDate<T>([NotNull] this IReadOnlyList<T> values,
            [NotNull] IReadOnlyList<Candlestick> candles, DateTime openTime)
        {
            if (candles.Count != values.Count)
            {
                throw new ArgumentException("Items counts should match");
            }

            var candle = candles.FirstOrDefault(c => c.OpenTime.GetTime() == openTime.ToUniversalTime());

            if (candle == null)
            {
                throw new ArgumentException("No candle with such date found");
            }

            var index = candles.ToList().IndexOf(candle);

            return values[index];
        }
    }
}