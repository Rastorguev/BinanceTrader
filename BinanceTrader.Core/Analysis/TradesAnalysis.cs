namespace BinanceTrader.Core.Analysis;

public class TradesAnalysis
{
    public IReadOnlyDictionary<string, AssetTradesAnalysis> AssetTradesAnalyses { get; }
    public decimal PnlNetTotal { get; }
    public decimal PnlNetAvg { get; }
    public decimal PnlNetMedian { get; }
    public decimal PnlGrossTotal { get; }
    public decimal PnlGrossAvg { get; }
    public decimal PnlGrossMedian { get; }
    public decimal PnlPercentageAvg { get; }
    public decimal PnlPercentageMedian { get; }
    public decimal FeeTotal { get; }
    public decimal FeeInQuoteTotal { get; }

    public TradesAnalysis(
        IReadOnlyDictionary<string, AssetTradesAnalysis> assetTradesAnalyses,
        decimal pnlNetTotal,
        decimal pnlNetAvg,
        decimal pnlNetMedian,
        decimal pnlGrossTotal,
        decimal pnlGrossAvg,
        decimal pnlGrossMedian,
        decimal pnlPercentageAvg,
        decimal pnlPercentageMedian,
        decimal feeTotal,
        decimal feeInQuoteTotal)
    {
        AssetTradesAnalyses = assetTradesAnalyses;
        PnlNetTotal = pnlNetTotal;
        PnlNetAvg = pnlNetAvg;
        PnlNetMedian = pnlNetMedian;
        PnlPercentageAvg = pnlPercentageAvg;
        PnlPercentageMedian = pnlPercentageMedian;
        PnlGrossTotal = pnlGrossTotal;
        PnlGrossAvg = pnlGrossAvg;
        PnlGrossMedian = pnlGrossMedian;
        FeeTotal = feeTotal;
        FeeInQuoteTotal = feeInQuoteTotal;
    }
}