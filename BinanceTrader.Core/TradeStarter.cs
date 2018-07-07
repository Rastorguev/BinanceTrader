using System.Collections.Generic;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using BinanceTrader.Tools.KeyProviders;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public class TradeStarter
    {
        [NotNull] private readonly ILogger _logger;

        public TradeStarter([NotNull] ILogger logger)
        {
            _logger = logger;
        }

        public async Task Start([NotNull] string traderName)
        {
            var connectionStringsProvider = new ConnectionStringsProvider();
            var keys = new BlobKeyProvider(connectionStringsProvider).GetKeys(traderName);
            var apiClient = new ApiClient(keys.Api, keys.Secret);
            var binanceClient = new BinanceClient(apiClient);
            var config = new BlobConfigProvider(connectionStringsProvider).GetConfig(traderName);
            var trader = new RabbitTrader(binanceClient, _logger, config);

            _logger.LogMessage("StartRequested", new Dictionary<string, string>
            {
                {"Api", keys.Api != null ? GetTruncatedKey(keys.Api) : "Invalid Api Key"},
                {"Secret", keys.Secret != null ? GetTruncatedKey(keys.Secret) : "Invalid Secret Key"},
                {"QuoteAsset", config.QuoteAsset},
                {"OrderExpiration", config.OrderExpiration.ToString()}
            });

            await trader.Start();
        }

        private static string GetTruncatedKey([NotNull] string key)
        {
            const int n = 5;

            if (key.Length < n * 2 + 10)
            {
                return key;
            }

            var truncated = $"{key.Substring(0, n)}...{key.Substring(key.Length - n, n)}";

            return truncated;
        }
    }
}