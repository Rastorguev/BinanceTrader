namespace BinanceTrader.Api
{
    public static class ApiUtils
    {
        public static string CreateCurrencySymbol(string baseAsset, string quoteAsset)
        {
            var currencyPair = string.Format($"{baseAsset}{quoteAsset}");
            return currencyPair;
        }
    }
}