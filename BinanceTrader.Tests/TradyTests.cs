using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.API.Csharp.Client.Models;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using JetBrains.Annotations;
using Trady.Analysis.Extension;

namespace BinanceTrader
{
    public class TradyTests
    {
        [NotNull] private readonly CandlesProvider _candlesProvider;

        public TradyTests([NotNull] CandlesProvider candlesProvider)
        {
            _candlesProvider = candlesProvider;
        }

        public async Task Execute()
        {
            var candles = await _candlesProvider.GetCandles(
                "ADA",
                "ETH",
                new DateTime(2018, 06, 23, 0, 0, 0),
                new DateTime(2018, 06, 23, 11, 0, 0),
                TimeInterval.Minutes_1);

            var closes = candles.Select(c => c.Close).ToList();
            var sma = closes.Ema(7).ToList();

            var s = sma.GetValueByDate(candles, new DateTime(2018, 06, 23, 11, 0, 0));
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