using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using BinanceTrader.Tools;
using BinanceTrader.Tools.KeyProviders;
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

            try
            {
                var keyProvider = new BlobKeyProvider(new ConnectionStringsProvider());
                var keys = keyProvider.GetKeys("Rambler");
                var apiClient = new ApiClient(keys.Api, keys.Secret);
                var binanceClient = new BinanceClient(apiClient);
                var config = new RabbitTraderConfig("ETH", TimeSpan.FromMinutes(5));

                var trader = new RabbitTrader(binanceClient, logger, config);

                trader.Start().Wait();
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