using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using BinanceTrader.Tools;
using JetBrains.Annotations;

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

            var assets = AssetsProvider.Assets;
            var configs = GetConfigs();
            var tests = new StrategiesTests(candlesProvider);

            var results = tests.CompareStrategies(assets, configs);

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

        [NotNull]
        [ItemNotNull]
        private static IReadOnlyList<TradeSessionConfig> GetConfigs()
        {
            TradeSessionConfig CreateConfig(decimal minProfit, decimal idle) =>
                new TradeSessionConfig(
                    initialQuoteAmount: 1m,
                    initialPrice: 0,
                    fee: 0.05m,
                    minQuoteAmount:
                    0.01m,
                    profitRatio: minProfit,
                    maxIdleHours: idle);

            var configs = new List<TradeSessionConfig>();

            const decimal profitStep = 0.5m;
            const decimal idleStep = 0.5m;

            var profit = 0.5m;
            while (profit <= 10)
            {
                var idle = 0.5m;
                while (idle <= 24m)
                {
                    configs.Add(CreateConfig(profit, idle));
                    idle += idleStep;
                }

                profit += profitStep;
            }

            return configs;
        }

        private static void PreventAppClose()
        {
            Task.Delay(-1).NotNull().Wait();
        }
    }
}