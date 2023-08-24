namespace BinanceTrader.Tools.KeyProviders;

public interface ITraderConfigProvider
{
    Task<IReadOnlyList<TraderConfig>> GetConfigsAsync();
}