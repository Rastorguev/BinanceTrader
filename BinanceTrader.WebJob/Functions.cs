using System;
using System.IO;
using BinanceTrader.Trader;
using JetBrains.Annotations;
using Microsoft.Azure.WebJobs;

namespace BinanceTrader.WebJob
{
    public class Functions
    {
        public static void ProcessQueueMessage([QueueTrigger("queue")] string message, [NotNull] TextWriter log)
        {
            log.WriteLine(message);
        }

        [NoAutomaticTrigger]
        public static void Start(TextWriter log)
        {
            var logger = new Logger();
            const string traderName = "Rambler";

            try
            {
                var starter = new TradeStarter(logger);
                starter.Start(traderName).Wait();
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }
        }
    }
}