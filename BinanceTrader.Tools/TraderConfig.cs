using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace BinanceTrader.Tools
{
    public class TraderConfig
    {
        [NotNull] public string Name { get; set; }
        public bool IsEnabled { get; set; }
        [NotNull] [ItemNotNull] public IReadOnlyList<string> BaseAssets { get; } = new List<string>();
        [NotNull] public string QuoteAsset { get; set; }
        public TimeSpan OrderExpiration { get; set; }
        public decimal ProfitRatio { get; set; }
    }
}