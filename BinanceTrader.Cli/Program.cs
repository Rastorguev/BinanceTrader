using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using BinanceTrader.Tools;
using BinanceTrader.Trader;

namespace BinanceTrader.Cli
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 5;

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var logger = new Logger();

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var keysPath = Path.Combine(Path.GetDirectoryName(assembly.Location).NotNull(), "Keys.config");
                var keyProvider = new BinanceKeyProvider(keysPath);
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

            PreventAppClose();
        }

        private static void PreventAppClose()
        {
            Task.Delay(-1).NotNull().Wait();
        }
    }
}