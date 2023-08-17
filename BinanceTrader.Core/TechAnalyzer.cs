using BinanceApi.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Core;

public static class TechAnalyzer
{
    public static decimal CalculateVolatilityIndex([NotNull] IEnumerable<Candlestick> candles)
    {
        var list = candles.ToList();

        return list.Any() ? list.Select(c => MathUtils.Gain(c.Low, c.High)).StandardDeviation() : 0;
    }
}