using JetBrains.Annotations;

namespace BinanceTrader.Tools.KeyProviders;

public interface IKeyProvider
{
    [NotNull]
    Task<IReadOnlyList<BinanceKeySet>> GetKeysAsync();
}

public class BinanceKeySet
{
    public string Name { get; set; }
    public string Api { get; set; }
    public string Secret { get; set; }
}