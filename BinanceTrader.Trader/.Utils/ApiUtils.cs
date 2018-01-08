namespace BinanceTrader.Utils
{
    public class ApiUtils
    {
        public static string CreateCurrencySymbol(string baseAsset, string quoteAsset)
        {
            var currencyPair = string.Format($"{baseAsset}{quoteAsset}");
            return currencyPair;
        }
    }
}