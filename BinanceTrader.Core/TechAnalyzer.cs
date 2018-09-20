using System.Collections.Generic;
using System.Linq;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public static class TechAnalyzer
    {
        public static decimal CalculateVolatility([NotNull] IEnumerable<Candlestick> candles)
        {
            var list = candles.ToList();

            if (!list.Any())
            {
                return 0;
            }

            var min = list.Select(c => c.NotNull().Low).Min();
            var max = list.Select(c => c.NotNull().High).Max();
            var volatility = MathUtils.Gain(min, max);

            return volatility;
        }
    }
}