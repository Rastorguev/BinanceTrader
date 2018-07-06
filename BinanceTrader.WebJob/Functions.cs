using System;
using System.IO;
using System.Reflection;
using Binance.API.Csharp.Client;
using BinanceTrader.Tools;
using BinanceTrader.Tools.KeyProviders;
using BinanceTrader.Trader;
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
                var assembly = Assembly.GetExecutingAssembly();
                var keysPath = Path.Combine(Path.GetDirectoryName(assembly.Location).NotNull(), "Configs", "Keys.config");
                var keyProvider = new ConfigFileKeyProvider(keysPath);
                var keys = keyProvider.GetKeys().NotNull();
                var apiClient = new ApiClient(keys.ApiKey, keys.SecretKey);
                var binanceClient = new BinanceClient(apiClient);

                var trader = new RabbitTrader(binanceClient, logger);
                trader.Start();
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }
        }
    }
}