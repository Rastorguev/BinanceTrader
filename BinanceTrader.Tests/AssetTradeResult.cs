using BinanceApi.Models.Account;
using BinanceTrader.Tools;

namespace BinanceTrader.Tests;

public class AssetTradeResult
{
    public AssetTradeResult(string baseAsset,
        decimal initialAmount,
        decimal tradeAmount,
        decimal holdAmount,
        int completedCount,
        int canceledCount,
        IReadOnlyList<Trade> trades,
        decimal feeAssetToQuoteConversionRatio)
    {
        BaseAsset = baseAsset;
        InitialAmount = initialAmount;
        TradeAmount = tradeAmount;
        HoldAmount = holdAmount;
        CompletedCount = completedCount;
        CanceledCount = canceledCount;
        TradeProfitPercentage = MathUtils.Gain(InitialAmount, TradeAmount);
        HoldProfitPercentage = MathUtils.Gain(InitialAmount, HoldAmount);
        Diff = MathUtils.Gain(HoldAmount, TradeAmount);
        Efficiency = TradeProfitPercentage - HoldProfitPercentage;
        Trades = trades;
        FeeAssetToQuoteConversionRatio = feeAssetToQuoteConversionRatio;
    }

    public string BaseAsset { get; }
    public decimal InitialAmount { get; }
    public decimal TradeAmount { get; }
    public decimal HoldAmount { get; }
    public decimal TradeProfitPercentage { get; }
    public decimal HoldProfitPercentage { get; }
    public decimal Diff { get; }
    public decimal Efficiency { get; }
    public int CompletedCount { get; }
    public int CanceledCount { get; }
    public IReadOnlyList<Trade> Trades { get; }
    public decimal FeeAssetToQuoteConversionRatio { get; }
}