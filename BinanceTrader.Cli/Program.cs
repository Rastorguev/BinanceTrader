using System.Collections.Generic;
using System.Globalization;
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
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var keyProvider = new BinanceKeyProvider(@"D:/Keys.config");
            var keys = keyProvider.GetKeys().NotNull();
            var apiClient = new ApiClient(keys.ApiKey, keys.SecretKey);
            var binanceClient = new BinanceClient(apiClient);

            var logger = new Logger();

            var trader = new RabbitTrader(binanceClient, logger);
            trader.Start();

            PreventAppClose();
        }

        private static void PreventAppClose()
        {
            Task.Delay(-1).NotNull().Wait();
        }
    }
}