namespace BinanceTrader.Tests;

public interface ITradeAccount
{
    decimal InitialBaseAmount { get; }
    decimal InitialQuoteAmount { get; }
    decimal CurrentBaseAmount { get; }
    decimal CurrentQuoteAmount { get; }
    public decimal TotalFee { get; }
    int CompletedCount { get; }
    int CanceledCount { get; }
    void Buy(decimal baseAmount, decimal price, DateTime time);
    void Sell(decimal baseAmount, decimal price, DateTime time);
    void Cancel();
}