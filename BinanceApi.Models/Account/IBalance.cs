namespace Binance.API.Csharp.Client.Models.Account
{
    public interface IBalance
    {
        string Asset { get; }
        decimal Free { get; }
        decimal Locked { get; }
    }
}