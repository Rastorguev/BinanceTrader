namespace BinanceTrader.Tests;

public class TradeSessionConfig
{
    public TradeSessionConfig(
        decimal initialQuoteAmount,
        decimal feePercentage,
        decimal feeAssetToQuoteConversionRatio,
        decimal minQuoteAmount,
        decimal profitRatio,
        TimeSpan maxIdle)
    {
        InitialQuoteAmount = initialQuoteAmount;
        FeePercentage = feePercentage;
        FeeAssetToQuoteConversionRatio = feeAssetToQuoteConversionRatio;
        MinQuoteAmount = minQuoteAmount;
        ProfitRatio = profitRatio;
        MaxIdlePeriod = maxIdle;
    }

    public decimal InitialQuoteAmount { get; }
    public decimal FeePercentage { get; }
    public decimal FeeAssetToQuoteConversionRatio { get; }
    public decimal MinQuoteAmount { get; }
    public decimal ProfitRatio { get; }
    public TimeSpan MaxIdlePeriod { get; }

    public override bool Equals(object obj)
    {
        return obj is TradeSessionConfig config &&
               InitialQuoteAmount == config.InitialQuoteAmount &&
               FeePercentage == config.FeePercentage &&
               MinQuoteAmount == config.MinQuoteAmount &&
               ProfitRatio == config.ProfitRatio &&
               MaxIdlePeriod == config.MaxIdlePeriod;
    }

    public override int GetHashCode()
    {
        var hashCode = 1639983505;
        hashCode = hashCode * -1521134295 + InitialQuoteAmount.GetHashCode();
        hashCode = hashCode * -1521134295 + FeePercentage.GetHashCode();
        hashCode = hashCode * -1521134295 + FeeAssetToQuoteConversionRatio.GetHashCode();
        hashCode = hashCode * -1521134295 + MinQuoteAmount.GetHashCode();
        hashCode = hashCode * -1521134295 + ProfitRatio.GetHashCode();
        hashCode = hashCode * -1521134295 + MaxIdlePeriod.GetHashCode();
        return hashCode;
    }
}