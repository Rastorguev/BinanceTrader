namespace BinanceApi.Models.Account
{
    public interface IBalance
    {
        string Asset { get; }
        decimal Free { get; }
        decimal Locked { get; }
    }
}