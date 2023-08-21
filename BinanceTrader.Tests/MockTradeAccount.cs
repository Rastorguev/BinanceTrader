using BinanceTrader.Tools;

namespace BinanceTrader.Tests;

public class MockTradeAccount : ITradeAccount
{
    private readonly decimal _feePercent;
    private readonly decimal _feeAssetToQuoteConversionRatio;

    public MockTradeAccount(
        decimal initialBaseAmount,
        decimal initialQuoteAmount,
        decimal feePercent,
        decimal feeAssetToQuoteConversionRatio)
    {
        CurrentBaseAmount = InitialBaseAmount = initialBaseAmount;
        CurrentQuoteAmount = InitialQuoteAmount = initialQuoteAmount;
        _feePercent = feePercent;
        _feeAssetToQuoteConversionRatio = feeAssetToQuoteConversionRatio;
    }

    public decimal InitialBaseAmount { get; }
    public decimal InitialQuoteAmount { get; }
    public decimal CurrentBaseAmount { get; private set; }
    public decimal CurrentQuoteAmount { get; private set; }
    public decimal TotalFee { get; private set; }
    public int CompletedCount { get; private set; }
    public int CanceledCount { get; private set; }

    public void Buy(decimal baseAmount, decimal price, DateTime time)
    {
        var quoteAmount = baseAmount * price;
        if (quoteAmount > CurrentQuoteAmount)
        {
            throw new Exception("Insufficient Balance");
        }

        CurrentQuoteAmount -= quoteAmount;
        CurrentBaseAmount += baseAmount;
        TotalFee += quoteAmount.Percents(_feePercent) / _feeAssetToQuoteConversionRatio;

        IncreaseCompletedCount();
    }

    public void Sell(decimal baseAmount, decimal price, DateTime time)
    {
        if (CurrentBaseAmount < baseAmount)
        {
            throw new Exception("Insufficient Balance");
        }

        var quoteAmount = baseAmount * price;
        CurrentBaseAmount -= baseAmount;
        CurrentQuoteAmount += quoteAmount;
        TotalFee += quoteAmount.Percents(_feePercent) / _feeAssetToQuoteConversionRatio;

        IncreaseCompletedCount();
    }

    public void Cancel()
    {
        CanceledCount++;
    }

    private void IncreaseCompletedCount()
    {
        CompletedCount++;
    }
}