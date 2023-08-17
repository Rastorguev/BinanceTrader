using BinanceTrader.Tools;

namespace BinanceTrader.Tests;

public interface ITradeAccount
{
    decimal InitialBaseAmount { get; }
    decimal InitialQuoteAmount { get; }
    decimal CurrentBaseAmount { get; }
    decimal CurrentQuoteAmount { get; }
    int CompletedCount { get; }
    int CanceledCount { get; }
    void IncreaseCompletedCount();
    void IncreaseCanceledCount();


    void Buy(decimal baseAmount, decimal price, DateTime timestamp);
    void Sell(decimal baseAmount, decimal price, DateTime timestamp);
}

public class MockTradeAccount : ITradeAccount
{
    private readonly decimal _fee;

    public MockTradeAccount(decimal initialBaseAmount, decimal initialQuoteAmount, decimal fee)
    {
        CurrentBaseAmount = InitialBaseAmount = initialBaseAmount;
        CurrentQuoteAmount = InitialQuoteAmount = initialQuoteAmount;
        _fee = fee;
    }

    public decimal InitialBaseAmount { get; }
    public decimal InitialQuoteAmount { get; }
    public decimal CurrentBaseAmount { get; private set; }
    public decimal CurrentQuoteAmount { get; private set; }
    public int CompletedCount { get; private set; }
    public int CanceledCount { get; private set; }

    public void Buy(decimal baseAmount, decimal price, DateTime timestamp)
    {
        var quoteAmount = baseAmount * price;
        if (quoteAmount > CurrentQuoteAmount)
        {
            throw new Exception("Insufficient Balance");
        }

        CurrentQuoteAmount -= quoteAmount;
        CurrentBaseAmount += baseAmount - baseAmount.Percents(_fee);

        IncreaseCompletedCount();
    }

    public void Sell(decimal baseAmount, decimal price, DateTime timestamp)
    {
        if (CurrentBaseAmount < baseAmount)
        {
            throw new Exception("Insufficient Balance");
        }

        var quoteAmount = baseAmount * price;
        CurrentBaseAmount -= baseAmount;
        CurrentQuoteAmount += quoteAmount - quoteAmount.Percents(_fee);

        IncreaseCompletedCount();
    }

    public void IncreaseCompletedCount()
    {
        CompletedCount++;
    }

    public void IncreaseCanceledCount()
    {
        CanceledCount++;
    }
}