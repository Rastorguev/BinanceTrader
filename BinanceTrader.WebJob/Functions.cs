using System;
using System.Collections.Generic;
using System.IO;
using Binance.API.Csharp.Client;
using BinanceTrader.Tools.KeyProviders;
using BinanceTrader.Trader;
using JetBrains.Annotations;
using Microsoft.Azure.WebJobs;

namespace BinanceTrader.WebJob
{
    public class Functions
    {
        public static void ProcessQueueMessage([QueueTrigger("queue")] string message, TextWriter log)
        {
            log.WriteLine(message);
        }

        [NoAutomaticTrigger]
        public static void Start(TextWriter log)
        {
            var logger = new Logger();

            try
            {
                var keyProvider = new BlobKeyProvider("Rambler");
                var keys = keyProvider.GetKeys();
                var apiClient = new ApiClient(keys.Api, keys.Secret);
                var binanceClient = new BinanceClient(apiClient);

                var trader = new RabbitTrader(binanceClient, logger);

                logger.LogMessage("StartRequested", new Dictionary<string, string>
                {
                    {"Api", keys.Api != null ? GetTruncatedKey(keys.Api) : "Invalid Api Key"},
                    {"Secret", keys.Secret != null ? GetTruncatedKey(keys.Secret) : "Invalid Secret Key"}
                });

                trader.Start().Wait();
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