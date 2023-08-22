using BinanceApi.Models.Account;
using BinanceApi.Models.Extensions;
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

    public static TradesAnalysis AnalyzeTradeHistory(
        Dictionary<string, IReadOnlyList<Trade>> tradeHistory, decimal feeAssetToQuoteConversionRatio)
    {
        var tradeResults = new Dictionary<string, AssetTradesAnalysis>();

        foreach (var assetTrades in tradeHistory)
        {
            var assetTradesAnalysis =
                GetAssetTradesAnalysis(assetTrades.Key, assetTrades.Value, feeAssetToQuoteConversionRatio);
            if (assetTradesAnalysis != null)
            {
                tradeResults.Add(assetTrades.Key, assetTradesAnalysis);
            }
        }

        var ordered = tradeResults
            .OrderByDescending(x => x.Value.PnlNetPercentage)
            .ToDictionary(x => x.Key, x => x.Value);

        var pnlNetTotal = ordered.Sum(x => x.Value.PnlNet).Round8();
        var pnlNetAvg = ordered.Select(x => x.Value.PnlNet).Average().Round8();
        var pnlNetMedian = ordered.Select(x => x.Value.PnlNet).Median().Round8();
        var pnlNetPercentageAvg = ordered.Select(x => x.Value.PnlNetPercentage).Average().Round8();
        var pnlNetPercentageMedian = ordered.Select(x => x.Value.PnlNetPercentage).Median().Round8();

        var pnlGrossTotal = ordered.Sum(x => x.Value.PnlGross).Round8();
        var pnlGrossAvg = ordered.Select(x => x.Value.PnlGross).Median().Round8();
        var pnlGrossMedian = ordered.Select(x => x.Value.PnlGross).Median().Round8();
        var pnlGrossPercentageAvg = ordered.Select(x => x.Value.PnlGrossPercentage).Average().Round8();
        var pnlGrossPercentageMedian = ordered.Select(x => x.Value.PnlGrossPercentage).Median().Round8();

        var feeTotal = ordered.Sum(x => x.Value.Fee).Round8();
        var feeInQuoteTotal = ordered.Sum(x => x.Value.FeeInQuote).Round8();

        var tradesAnalysis = new TradesAnalysis(
            ordered,
            pnlNetTotal,
            pnlNetAvg,
            pnlNetMedian,
            pnlNetPercentageAvg,
            pnlNetPercentageMedian,
            pnlGrossTotal,
            pnlGrossAvg,
            pnlGrossMedian,
            pnlGrossPercentageAvg,
            pnlGrossPercentageMedian,
            feeTotal,
            feeInQuoteTotal
        );

        return tradesAnalysis;
    }

    private static AssetTradesAnalysis GetAssetTradesAnalysis(
        string baseAsset, IReadOnlyList<Trade> trades, decimal feeAssetToQuoteConversionRatio)
    {
        var buyTrades = trades.Where(x => x.IsBuyer == true).ToList();
        var sellTrades = trades.Where(x => x.IsBuyer == false).ToList();

        if (!sellTrades.Any())
        {
            return null;
        }

        var buySum = buyTrades.Sum(x => x.Quantity * x.Price).Round8();
        var sellSum = sellTrades.Sum(x => x.Quantity * x.Price).Round8();

        var buyQty = buyTrades.Sum(x => x.Quantity).Round8();
        var sellQty = sellTrades.Sum(x => x.Quantity).Round8();

        var buyAvgPrice = (buySum / buyQty).Round8();
        var sellAvgPrice = (sellSum / sellQty).Round8();

        var fee = trades.Sum(x => x.Commission).Round8();
        var feeInQuote = (fee * feeAssetToQuoteConversionRatio).Round8();

        var pnlGross = (sellQty * (sellAvgPrice - buyAvgPrice)).Round8();
        var pnlNet = (pnlGross - feeInQuote).Round8();
        var pnlNetPercentage = ((sellQty * sellAvgPrice / (sellQty * buyAvgPrice) - 1) * 100).Round8();
        var pnlGrossPercentage = ((sellQty * sellAvgPrice / (sellQty * buyAvgPrice + feeInQuote) - 1) * 100).Round8();

        var analysis = new AssetTradesAnalysis(
            baseAsset,
            trades.Count,
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
            pnlNetPercentage,
            pnlGrossPercentage
        );

        return analysis;
    }
}