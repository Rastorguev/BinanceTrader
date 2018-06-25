using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using BinanceTrader.Tools;

namespace BinanceTrader
{
    internal class Program
    {
        private static void Main()
        {
            ServicePointManager.DefaultConnectionLimit = 10;

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var keyProvider = new BinanceKeyProvider(@"D:/Keys.config");
            var keys = keyProvider.GetKeys().NotNull();
            var apiClient = new ApiClient(keys.ApiKey, keys.SecretKey);
            var binanceClient = new BinanceClient(apiClient);
            var candlesProvider = new CandlesProvider(binanceClient);

            var tests = new StrategiesTests(candlesProvider);
            var results = tests.CompareStrategies();

            foreach (var result in results)
            {
                var tradesResult = result.Value.NotNull();

                Console.WriteLine($"{result.Key.NotNull().ProfitRatio} / {result.Key.NotNull().MaxIdleHours}");
                Console.WriteLine();
                Console.WriteLine($"Initial Total:\t\t {tradesResult.InitialAmount.Round()}");
                Console.WriteLine($"Trade Total:\t\t {tradesResult.TradeAmount.Round()}");
                Console.WriteLine($"Hold Total:\t\t {tradesResult.HoldAmount.Round()}");
                Console.WriteLine($"Trade Profit Total %:\t {tradesResult.TradeProfit.Round()}");
                Console.WriteLine($"Hold Profit Total %:\t {tradesResult.HoldProfit.Round()}");
                Console.WriteLine($"Diff %:\t\t {tradesResult.Diff.Round()}");
                Console.WriteLine($"Afficiency:\t {tradesResult.Afficiency.Round()}");
                Console.WriteLine("----------------------");
                Console.WriteLine();
            }

            PreventAppClose();
        }

        private static void PreventAppClose()
        {
            Task.Delay(-1).Wait();
        }
    }
}