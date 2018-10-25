using System.Collections.Generic;
using JetBrains.Annotations;

namespace BinanceTrader.Tools.KeyProviders
{
    public interface IKeyProvider
    {
        [NotNull]
        IReadOnlyList<BinanceKeySet> GetKeys();
    }

    public class BinanceKeySet
    {
        public string Name { get; set; }
        public string Api { get; set; }
        public string Secret { get; set; }
    }
}