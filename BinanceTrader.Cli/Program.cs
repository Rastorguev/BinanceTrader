using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using BinanceTrader.Tools;
using BinanceTrader.Trader;

namespace BinanceTrader.Cli
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 10;

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

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

            PreventAppClose();
        }

        private static void PreventAppClose()
        {
            Task.Delay(-1).NotNull().Wait();
        }
    }
}