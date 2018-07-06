namespace BinanceTrader.Tools.KeyProviders
{
    public interface IKeyProvider
    {
        BinanceKeys GetKeys();
    }

    public class BinanceKeys
    {
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
    }
}