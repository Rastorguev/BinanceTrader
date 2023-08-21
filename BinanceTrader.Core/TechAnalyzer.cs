using BinanceApi.Models.Account;
using BinanceApi.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Core;

public static partial class TechAnalyzer
{
    public static decimal CalculateVolatilityIndex([NotNull] IEnumerable<Candlestick> candles)
    {
        var list = candles.ToList();

        return list.Any() ? list.Select(c => MathUtils.Gain(c.Low, c.High)).StandardDeviation() : 0;
    }

    public static IReadOnlyDictionary<string, AssetTradesAnalysis> AnalyzeTradeHistory(
        Dictionary<string, IReadOnlyList<Trade>> tradeHistory, decimal feeAssetToQuoteConversionRatio)
    {
        var result = new Dictionary<string, AssetTradesAnalysis>();

        foreach (var assetTrades in tradeHistory)
        {
            var buyTrades = assetTrades.Value.Where(x => x.IsBuyer == true).ToList();
            var sellTrades = assetTrades.Value.Where(x => x.IsBuyer == false).ToList();

            if (!sellTrades.Any())
            {
                continue;
            }

            var buySum = buyTrades.Sum(x => x.Quantity * x.Price);
            var sellSum = sellTrades.Sum(x => x.Quantity * x.Price);

            var buyQty = buyTrades.Sum(x => x.Quantity);
            var sellQty = sellTrades.Sum(x => x.Quantity);

            var buyAvgPrice = buySum / buyQty;
            var sellAvgPrice = sellSum / sellQty;

            var fee = assetTrades.Value.Sum(x => x.Commission);
            var feeInQuote = fee * feeAssetToQuoteConversionRatio;

            var pnlGross = sellQty * (sellAvgPrice - buyAvgPrice);
            var pnlNet = pnlGross - feeInQuote;
            var pnlNetPercent = ((sellQty * sellAvgPrice) / (sellQty * buyAvgPrice) - 1) * 100;
            var pnlGrossPercent = ((sellQty * sellAvgPrice) / ((sellQty * buyAvgPrice) + feeInQuote) - 1) * 100;

            var analysis = new AssetTradesAnalysis(
                assetTrades.Key,
                assetTrades.Value.Count,
                fee,
                feeInQuote,
                buySum,
                sellSum,
                buyQty,
                sellQty,
                buyAvgPrice,
                sellAvgPrice,
                pnlNet,
                pnlGross,
                pnlNetPercent,
                pnlGrossPercent
            );

            result.Add(assetTrades.Key, analysis);
        }

        return result
            .OrderByDescending(x => x.Value.PnlNetPercent)
            .ToDictionary(x => x.Key, x => x.Value);
    }
}