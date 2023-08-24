using BinanceApi.Models.Account;
using BinanceApi.Models.Extensions;
using BinanceApi.Models.Market;
using BinanceTrader.Tools;

namespace BinanceTrader.Core;

public static class TechAnalyzer
{
    public static decimal CalculateVolatilityIndex(IEnumerable<Candlestick> candles)
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

        var orderedByPnlNet = tradeResults
            .OrderByDescending(x => x.Value.PnlNet)
            .ToDictionary(x => x.Key, x => x.Value);

        var pnlNetTotal = orderedByPnlNet.Sum(x => x.Value.PnlNet).Round8();
        var pnlNetAvg = orderedByPnlNet.Select(x => x.Value.PnlNet).Average().Round8();
        var pnlNetMedian = orderedByPnlNet.Select(x => x.Value.PnlNet).Median().Round8();

        var pnlGrossTotal = orderedByPnlNet.Sum(x => x.Value.PnlGross).Round8();
        var pnlGrossAvg = orderedByPnlNet.Select(x => x.Value.PnlGross).Average().Round8();
        var pnlGrossMedian = orderedByPnlNet.Select(x => x.Value.PnlGross).Median().Round8();

        var pnlPercentageAvg = orderedByPnlNet.Select(x => x.Value.PnlPercentage).Average().Round8();
        var pnlPercentageMedian = orderedByPnlNet.Select(x => x.Value.PnlPercentage).Median().Round8();

        var feeTotal = orderedByPnlNet.Sum(x => x.Value.Fee).Round8();
        var feeInQuoteTotal = orderedByPnlNet.Sum(x => x.Value.FeeInQuote).Round8();

        var tradesAnalysis = new TradesAnalysis(
            orderedByPnlNet,
            pnlNetTotal,
            pnlNetAvg,
            pnlNetMedian,
            pnlGrossTotal,
            pnlGrossAvg,
            pnlGrossMedian,
            pnlPercentageAvg,
            pnlPercentageMedian,
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
        var pnlPercentage = ((sellQty * sellAvgPrice / (sellQty * buyAvgPrice) - 1) * 100).Round8();

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
            pnlPercentage
        );

        return analysis;
    }
}