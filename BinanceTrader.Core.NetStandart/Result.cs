namespace BinanceTrader.Trader;

public class Result<T>
{
    public Result(bool success, T value)
    {
        Success = success;
        Value = value;
    }

    public bool Success { get; }
    public T Value { get; }
}