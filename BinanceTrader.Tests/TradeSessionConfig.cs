namespace BinanceTrader.Tests;

public class TradeSessionConfig
{
    public TradeSessionConfig(
        decimal initialQuoteAmount,
        decimal feePercent,
        decimal feeAssetToQuoteConversionRatio,
        decimal minQuoteAmount,
        decimal profitRatio,
        TimeSpan maxIdle)
    {
        InitialQuoteAmount = initialQuoteAmount;
        FeePercent = feePercent;
        FeeAssetToQuoteConversionRatio = feeAssetToQuoteConversionRatio;
        MinQuoteAmount = minQuoteAmount;
        ProfitRatio = profitRatio;
        MaxIdlePeriod = maxIdle;
    }

    public decimal InitialQuoteAmount { get; }
    public decimal FeePercent { get; }
    public decimal FeeAssetToQuoteConversionRatio { get; }
    public decimal MinQuoteAmount { get; }
    public decimal ProfitRatio { get; }
    public TimeSpan MaxIdlePeriod { get; }

    public override bool Equals(object obj)
    {
        return obj is TradeSessionConfig config &&
               InitialQuoteAmount == config.InitialQuoteAmount &&
               FeePercent == config.FeePercent &&
               MinQuoteAmount == config.MinQuoteAmount &&
               ProfitRatio == config.ProfitRatio &&
               MaxIdlePeriod == config.MaxIdlePeriod;
    }

    public override int GetHashCode()
    {
        var hashCode = 1639983505;
        hashCode = hashCode * -1521134295 + InitialQuoteAmount.GetHashCode();
        hashCode = hashCode * -1521134295 + FeePercent.GetHashCode();
        hashCode = hashCode * -1521134295 + FeeAssetToQuoteConversionRatio.GetHashCode();
        hashCode = hashCode * -1521134295 + MinQuoteAmount.GetHashCode();
        hashCode = hashCode * -1521134295 + ProfitRatio.GetHashCode();
        hashCode = hashCode * -1521134295 + MaxIdlePeriod.GetHashCode();
        return hashCode;
    }
}