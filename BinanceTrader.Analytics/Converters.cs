using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Tools;
using JetBrains.Annotations;
using Trady.Core;

namespace BinanceTrader.Analytics
{
    public static class Converters
    {
        [NotNull]
        public static List<Candle> ToTradyCandles(
            [NotNull] [ItemNotNull] this IEnumerable<Core.Entities.Candle> candles)
        {
            return candles.Select(c => c.ToTradyCandle()).ToList().NotNull();
        }

        public static Candle ToTradyCandle([NotNull] this Core.Entities.Candle candle) =>
            new Candle(
                candle.OpenTime,
                candle.OpenPrice,
                candle.HighPrice,
                candle.LowPrice,
                candle.ClosePrice,
                candle.Volume);
    }
}