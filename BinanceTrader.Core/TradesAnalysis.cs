namespace BinanceTrader.Core;

public class TradesAnalysis
{
    public IReadOnlyDictionary<string, AssetTradesAnalysis> AssetTradesAnalyses { get; }
    public decimal PnlNetTotal { get; }
    public decimal PnlNetAvg { get; }
    public decimal PnlNetMedian { get; }
    public decimal PnlNetPercentAvg { get; }
    public decimal PnlNetPercentMedian { get; }
    public decimal PnlGrossTotal { get; }
    public decimal PnlGrossAvg { get; }
    public decimal PnlGrossMedian { get; }
    public decimal PnlGrossPercentAvg { get; }
    public decimal PnlGrossPercentMedian { get; }
    public decimal FeeTotal { get; }
    public decimal FeeInQuoteTotal { get; }

    public TradesAnalysis(
        IReadOnlyDictionary<string, AssetTradesAnalysis> assetTradesAnalyses,
        decimal pnlNetTotal,
        decimal pnlNetAvg,
        decimal pnlNetMedian,
        decimal pnlNetPercentAvg,
        decimal pnlNetPercentMedian,
        decimal pnlGrossTotal,
        decimal pnlGrossAvg,
        decimal pnlGrossMedian,
        decimal pnlGrossPercentAvg,
        decimal pnlGrossPercentMedian,
        decimal feeTotal,
        decimal feeInQuoteTotal)
    {
        AssetTradesAnalyses = assetTradesAnalyses;
        PnlNetTotal = pnlNetTotal;
        PnlNetAvg = pnlNetAvg;
        PnlNetMedian = pnlNetMedian;
        PnlNetPercentAvg = pnlNetPercentAvg;
        PnlNetPercentMedian = pnlNetPercentMedian;
        PnlGrossTotal = pnlGrossTotal;
        PnlGrossAvg = pnlGrossAvg;
        PnlGrossMedian = pnlGrossMedian;
        PnlGrossPercentAvg = pnlGrossPercentAvg;
        PnlGrossPercentMedian = pnlGrossPercentMedian;
        FeeTotal = feeTotal;
        FeeInQuoteTotal = feeInQuoteTotal;
    }
}