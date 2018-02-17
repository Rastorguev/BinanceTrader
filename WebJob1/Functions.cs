using System.IO;
using Binance.API.Csharp.Client;
using BinanceTrader.Tools;
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
            var keyProvider = new BinanceKeyProvider(@"D:/Keys.config");
            var keys = keyProvider.GetKeys().NotNull();
            var apiClient = new ApiClient(keys.ApiKey, keys.SecretKey);
            var binanceClient = new BinanceClient(apiClient);

            var logger = new Logger();
            var trader = new RabbitTrader(binanceClient, logger);
            trader.Start();
        }
    }
}