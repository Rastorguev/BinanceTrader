using BinanceApi.Models.Account;
using BinanceTrader.Tools;

namespace BinanceTrader.Tests;

public class MockTradeAccount : ITradeAccount
{
    private readonly decimal _feePercent;
    private readonly decimal _feeAssetToQuoteConversionRatio;
    private readonly List<Trade> _trades = new List<Trade>();

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
    public IReadOnlyList<Trade> Trades => _trades;

    public void Buy(decimal baseAmount, decimal price, DateTime time)
    {
        var quoteAmount = baseAmount * price;
        if (quoteAmount > CurrentQuoteAmount)
        {
            throw new Exception("Insufficient Balance");
        }

        var fee = quoteAmount.Percents(_feePercent) / _feeAssetToQuoteConversionRatio;

        CurrentQuoteAmount -= quoteAmount;
        CurrentBaseAmount += baseAmount;

        TotalFee += fee;

        AddTrade(true, baseAmount, price, time, fee);

        IncreaseCompletedCount();
    }

    public void Sell(decimal baseAmount, decimal price, DateTime time)
    {
        if (CurrentBaseAmount < baseAmount)
        {
            throw new Exception("Insufficient Balance");
        }

        var quoteAmount = baseAmount * price;
        var fee = quoteAmount.Percents(_feePercent) / _feeAssetToQuoteConversionRatio;

        CurrentBaseAmount -= baseAmount;
        CurrentQuoteAmount += quoteAmount;
        TotalFee += fee;

        AddTrade(false, baseAmount, price, time, fee);

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

    private void AddTrade(bool isBuyer, decimal baseAmount, decimal price, DateTime time, decimal fee)
    {
        var unixTime = new DateTimeOffset(time).ToUnixTimeMilliseconds();

        _trades.Add(new Trade()
        {
            UnixTime = unixTime,
            IsBuyer = isBuyer,
            Quantity = baseAmount,
            Price = price,
            Commission = fee
        });
    }
}