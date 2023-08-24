namespace BinanceTrader.Core;

public static class SymbolUtils
{
    public static string GetCurrencySymbol(string baseAsset, string quoteAsset)
    {
        return string.Format($"{baseAsset}{quoteAsset}");
    }

    public static string GetBaseAsset(string symbol, string quoteAsset)
    {
        if (!symbol.Contains(quoteAsset))
        {
            throw new ArgumentException("Symbol doesn't contain QuoteAsset");
        }

        return symbol.Replace(quoteAsset, string.Empty);
    }
}