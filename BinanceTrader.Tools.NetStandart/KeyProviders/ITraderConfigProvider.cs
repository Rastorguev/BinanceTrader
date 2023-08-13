using System.Collections.Generic;
using JetBrains.Annotations;

namespace BinanceTrader.Tools.KeyProviders
{
    public interface ITraderConfigProvider
    {
        [NotNull]
        Task<IReadOnlyList<TraderConfig>> GetConfigsAsync();
    }
}