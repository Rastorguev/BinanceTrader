using System;
using JetBrains.Annotations;

namespace BinanceTrader.Tools
{
    public class TraderConfig
    {
        [NotNull]
        public string QuoteAsset { get; }
        public TimeSpan OrderExpiration { get; }
        public decimal ProfitRatio { get; }

        public TraderConfig([NotNull] string quoteAsset, TimeSpan orderExpiration, decimal profitRatio)
        {
            QuoteAsset = quoteAsset;
            OrderExpiration = orderExpiration;
            ProfitRatio = profitRatio;
        }
    }
}