using BinanceTrader.Core;
using BinanceTrader.Tools;

namespace BinanceTrader.Tests;

public class TradeResult
{
    public TradeResult(
        decimal initialAmount,
        decimal tradeAmount,
        decimal holdAmount,
        int completedCount,
        int canceledCount,
        TradesAnalysis tradeAnalysis)
    {
        InitialAmount = initialAmount;
        TradeAmount = tradeAmount;
        HoldAmount = holdAmount;
        CompletedCount = completedCount;
        CanceledCount = canceledCount;
        TradeProfitPercentage = MathUtils.Gain(InitialAmount, TradeAmount);
        HoldProfitPercentage = MathUtils.Gain(InitialAmount, HoldAmount);
        TradeHoldDiffPercentage = MathUtils.Gain(HoldAmount, TradeAmount);
        Efficiency = TradeProfitPercentage - HoldProfitPercentage;
        TradeAnalysis = tradeAnalysis;
    }

    public decimal InitialAmount { get; }
    public decimal TradeAmount { get; }
    public decimal HoldAmount { get; }
    public decimal TradeProfitPercentage { get; }
    public decimal HoldProfitPercentage { get; }
    public decimal TradeHoldDiffPercentage { get; }
    public decimal Efficiency { get; }
    public int CompletedCount { get; }
    public int CanceledCount { get; }
    public TradesAnalysis TradeAnalysis { get; }
}