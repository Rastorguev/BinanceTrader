using System;
using System.Collections.Generic;
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
            try
            {
                var traders = new List<string>
                {
                    "Rambler",
                    "Google"
                };

                TradeStarter.Start(traders, traderName => new Logger(traderName));
            }
            catch (Exception ex)
            {
                new Logger(string.Empty).LogException(ex);
            }

        }
    }
}