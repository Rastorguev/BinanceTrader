using System;
using JetBrains.Annotations;

namespace BinanceTrader.Tools
{
    public class TraderConfig
    {
        public bool IsEnabled { get; }
        [NotNull]
        public string QuoteAsset { get; }
        public TimeSpan OrderExpiration { get; }
        public decimal ProfitRatio { get; }

        public TraderConfig(
            bool isEnabled,
            [NotNull] string quoteAsset,
            TimeSpan orderExpiration,
            decimal profitRatio)
        {
            IsEnabled = isEnabled;
            QuoteAsset = quoteAsset;
            OrderExpiration = orderExpiration;
            ProfitRatio = profitRatio;
        }
    }
}