namespace BinanceTrader.Core;

public class TradesAnalysis
{
    public IReadOnlyDictionary<string, AssetTradesAnalysis> AssetTradesAnalyses { get; }
    public decimal PnlNetTotal { get; }
    public decimal PnlNetAvg { get; }
    public decimal PnlNetMedian { get; }
    public decimal PnlNetPercentageAvg { get; }
    public decimal PnlNetPercentageMedian { get; }
    public decimal PnlGrossTotal { get; }
    public decimal PnlGrossAvg { get; }
    public decimal PnlGrossMedian { get; }
    public decimal PnlGrossPercentageAvg { get; }
    public decimal PnlGrossPercentageMedian { get; }
    public decimal FeeTotal { get; }
    public decimal FeeInQuoteTotal { get; }

    public TradesAnalysis(
        IReadOnlyDictionary<string, AssetTradesAnalysis> assetTradesAnalyses,
        decimal pnlNetTotal,
        decimal pnlNetAvg,
        decimal pnlNetMedian,
        decimal pnlNetPercentageAvg,
        decimal pnlNetPercentageMedian,
        decimal pnlGrossTotal,
        decimal pnlGrossAvg,
        decimal pnlGrossMedian,
        decimal pnlGrossPercentageAvg,
        decimal pnlGrossPercentageMedian,
        decimal feeTotal,
        decimal feeInQuoteTotal)
    {
        AssetTradesAnalyses = assetTradesAnalyses;
        PnlNetTotal = pnlNetTotal;
        PnlNetAvg = pnlNetAvg;
        PnlNetMedian = pnlNetMedian;
        PnlNetPercentageAvg = pnlNetPercentageAvg;
        PnlNetPercentageMedian = pnlNetPercentageMedian;
        PnlGrossTotal = pnlGrossTotal;
        PnlGrossAvg = pnlGrossAvg;
        PnlGrossMedian = pnlGrossMedian;
        PnlGrossPercentageAvg = pnlGrossPercentageAvg;
        PnlGrossPercentageMedian = pnlGrossPercentageMedian;
        FeeTotal = feeTotal;
        FeeInQuoteTotal = feeInQuoteTotal;
    }
}