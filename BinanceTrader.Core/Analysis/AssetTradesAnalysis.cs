namespace BinanceTrader.Core;

public class AssetTradesAnalysis
{
    public string BaseAsset { get; }
    public int TradesCount { get; }
    public decimal Fee { get; }
    public decimal FeeInQuote { get; }
    public decimal BuySum { get; }
    public decimal SellSum { get; }
    public decimal BuyQty { get; }
    public decimal SellQty { get; }
    public decimal BuyAvgPrice { get; }
    public decimal SellAvgPrice { get; }
    public decimal PnlNet { get; }
    public decimal PnlGross { get; }
    public decimal PnlPercentage { get; }

    public AssetTradesAnalysis(
        string baseAsset,
        int tradesCount,
        decimal fee,
        decimal feeInQuote,
        decimal buySum,
        decimal sellSum,
        decimal buyQty,
        decimal sellQty,
        decimal buyAvgPrice,
        decimal sellAvgPrice,
        decimal pnlNet,
        decimal pnlGross,
        decimal pnlPercentage)
    {
        BaseAsset = baseAsset;
        TradesCount = tradesCount;
        Fee = fee;
        FeeInQuote = feeInQuote;
        BuySum = buySum;
        SellSum = sellSum;
        BuyQty = buyQty;
        SellQty = sellQty;
        BuyAvgPrice = buyAvgPrice;
        SellAvgPrice = sellAvgPrice;
        PnlNet = pnlNet;
        PnlGross = pnlGross;
        PnlPercentage = pnlPercentage;
    }
}