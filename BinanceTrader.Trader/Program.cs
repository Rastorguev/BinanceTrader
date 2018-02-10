using System;
using System.Globalization;
using Binance.API.Csharp.Client;
using BinanceTrader.Api;
using BinanceTrader.Tools;

namespace BinanceTrader
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

            var trader = new Trader(binanceClient);

            trader.Start();

            //var keyProvider = new MockKeyProvider();
            //var test = new StrategiesTests(new BinanceApi(keyProvider));
            //test.CompareStrategies();

            PreventAppClose();
        }

        private static void PreventAppClose()
        {
            while (true)
            {
                Console.ReadKey();
            }
        }
    }
}