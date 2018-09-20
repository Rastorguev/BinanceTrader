using System;
using System.Collections.Generic;
using System.Globalization;
using Binance.API.Csharp.Client;
using BinanceTrader.Tools;
using BinanceTrader.Tools.KeyProviders;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public class TradeStarter
    {
        public static void Start(
            [NotNull] [ItemNotNull] IEnumerable<string> traderNames,
            [NotNull] Func<string, ILogger> loggerResolver)
        {
            foreach (var traderName in traderNames)
            {
                Start(traderName, loggerResolver);
            }
        }

        public static async void Start(
            [NotNull] string traderName,
            [NotNull] Func<string, ILogger> loggerResolver)
        {
            var logger = loggerResolver(traderName).NotNull();

            try
            {
                var connectionStringsProvider = new ConnectionStringsProvider();
                var keys = new BlobKeyProvider(connectionStringsProvider).GetKeys(traderName);
                var apiClient = new ApiClient(keys.Api, keys.Secret);
                var binanceClient = new BinanceClient(apiClient);
                var config = new BlobConfigProvider(connectionStringsProvider).GetConfig(traderName);
                var candlesProvider = new CandlesLoader(binanceClient);
                var volatilityChecker = new VolatilityChecker(candlesProvider, logger);
                var trader = new RabbitTrader(binanceClient, logger, config, volatilityChecker);

                logger.LogMessage("StartRequested", new Dictionary<string, string>
                {
                    {"Api", keys.Api != null ? GetTruncatedKey(keys.Api) : "Invalid Api Key"},
                    {"Secret", keys.Secret != null ? GetTruncatedKey(keys.Secret) : "Invalid Secret Key"},
                    {"IsEnabled", config.IsEnabled.ToString()},
                    {"QuoteAsset", config.QuoteAsset},
                    {"OrderExpiration", config.OrderExpiration.ToString()},
                    {"ProfitRatio", config.ProfitRatio.ToString(CultureInfo.InvariantCulture)}
                });

                if (config.IsEnabled)
                {
                    await trader.Start();
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }
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