using System;
using System.Collections.Generic;
using System.Linq;
using Binance.API.Csharp.Client.Models;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;
using Trady.Analysis;
using Trady.Core;
using Trady.Core.Infrastructure;

namespace BinanceTrader
{
    public static class CandlesExtensions
    {
        [NotNull]
        public static IOhlcv ToIOhlcv([NotNull] this Candlestick candlestick)
        {
            return new Candle(
                new DateTimeOffset(candlestick.OpenTime.GetTime()),
                candlestick.Open,
                candlestick.High,
                candlestick.Low,
                candlestick.Close,
                candlestick.Volume
            );
        }

        [NotNull]
        [ItemNotNull]
        public static IReadOnlyList<IOhlcv> ToIOhlcvList([NotNull] this IEnumerable<Candlestick> candlesticks)
        {
            return candlesticks.Select(c => c.NotNull().ToIOhlcv()).ToList();
        }

        [NotNull]
        [ItemNotNull]
        public static IReadOnlyList<IIndexedOhlcv> ToIndexedToIOhlcvList(
            [NotNull] this IEnumerable<Candlestick> candlesticks)
        {
            var list = candlesticks.ToList();
            var indexedCandles = new List<IIndexedOhlcv>();
            var candles = list.ToIOhlcvList();

            for (var i = 0; i < list.Count(); i++)
            {
                indexedCandles.Add(new IndexedCandle(candles, i));
            }

            return indexedCandles;
        }
    }
}