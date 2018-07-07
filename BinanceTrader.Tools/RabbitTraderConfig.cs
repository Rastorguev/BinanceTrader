using System;
using JetBrains.Annotations;

namespace BinanceTrader.Tools
{
    public class RabbitTraderConfig
    {
        [NotNull]
        public string QuoteAsset { get; }
        public TimeSpan OrderExpiration { get; }

        public RabbitTraderConfig([NotNull] string quoteAsset, TimeSpan orderExpiration)
        {
            QuoteAsset = quoteAsset;
            OrderExpiration = orderExpiration;
        }
    }
}