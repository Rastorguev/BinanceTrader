using JetBrains.Annotations;

namespace BinanceTrader.Trader;

public static class SymbolUtils
{
    public static string GetCurrencySymbol([NotNull] string baseAsset, [NotNull] string quoteAsset)
    {
        return string.Format($"{baseAsset}{quoteAsset}");
    }

    public static string GetBaseAsset([NotNull] string symbol, [NotNull] string quoteAsset)
    {
        if (!symbol.Contains(quoteAsset))
        {
            throw new ArgumentException("Symbol doesn't contain QuoteAsset");
        }

        return symbol.Replace(quoteAsset, string.Empty);
    }
}