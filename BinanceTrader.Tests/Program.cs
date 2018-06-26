using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Enums;
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

            var watch = Stopwatch.StartNew();

            var results = tests.CompareStrategies(assets,
                    "ETH",
                    //new DateTime(2017, 09, 01, 0, 0, 0),
                    //new DateTime(2018, 06, 25, 9, 0, 0),    
                    new DateTime(2018, 06, 01, 0, 0, 0),
                    new DateTime(2018, 06, 15, 0, 0, 0),
                    TimeInterval.Minutes_1,
                    configs)
                .Result;

            watch.Stop();
            var elapsed = new TimeSpan(watch.ElapsedTicks);

            var ordered = results.NotNull().OrderByDescending(r => r.Value.NotNull().TradeProfit).ToList();
            var max = ordered.First();
            var min = ordered.First();
            var x2_12 = ordered.First(r => r.Key.NotNull().ProfitRatio == 2 && r.Key.NotNull().MaxIdleHours == 12);
            var x1_1 = ordered.First(r => r.Key.NotNull().ProfitRatio == 1 && r.Key.NotNull().MaxIdleHours == 1);
            var x1_12 = ordered.First(r => r.Key.NotNull().ProfitRatio == 1 && r.Key.NotNull().MaxIdleHours == 12);
            var x05_4 = ordered.First(r => r.Key.NotNull().ProfitRatio == 0.5m && r.Key.NotNull().MaxIdleHours == 4);
            var diff1 = MathUtils.Gain(x2_12.Value.NotNull().TradeProfit, max.Value.NotNull().TradeProfit);
            var diff2 = MathUtils.Gain(x2_12.Value.NotNull().TradeProfit, x1_1.Value.NotNull().TradeProfit);
            var diff3 = MathUtils.Gain(x1_1.Value.NotNull().TradeProfit, max.Value.NotNull().TradeProfit);
            var diff4 = MathUtils.Gain(x05_4.Value.NotNull().TradeProfit, max.Value.NotNull().TradeProfit);

            var i2_12 = ordered.IndexOf(x2_12);
            var i1_1 = ordered.IndexOf(x1_1);
            var i1_12 = ordered.IndexOf(x1_12);
            var i05_4 = ordered.IndexOf(x05_4);

            Console.WriteLine($"Elapsed Time: {elapsed.TotalSeconds}");

            Debugger.Break();

            //foreach (var result in results)
            //{
            //    var tradesResult = result.Value.NotNull();

            //    Console.WriteLine($"{result.Key.NotNull().ProfitRatio} / {result.Key.NotNull().MaxIdleHours}");
            //    Console.WriteLine();
            //    Console.WriteLine($"Initial Total:\t\t {tradesResult.InitialAmount.Round()}");
            //    Console.WriteLine($"Trade Total:\t\t {tradesResult.TradeAmount.Round()}");
            //    Console.WriteLine($"Hold Total:\t\t {tradesResult.HoldAmount.Round()}");
            //    Console.WriteLine($"Trade Profit Total %:\t {tradesResult.TradeProfit.Round()}");
            //    Console.WriteLine($"Hold Profit Total %:\t {tradesResult.HoldProfit.Round()}");
            //    Console.WriteLine($"Diff %:\t\t {tradesResult.Diff.Round()}");
            //    Console.WriteLine($"Afficiency:\t {tradesResult.Afficiency.Round()}");
            //    Console.WriteLine("----------------------");
            //    Console.WriteLine();
            //}

            PreventAppClose();
        }

        [NotNull]
        [ItemNotNull]
        private static IReadOnlyList<TradeSessionConfig> GetConfigs()
        {
            TradeSessionConfig CreateConfig(decimal minProfit, decimal idle) =>
                new TradeSessionConfig(
                    1m,
                    0.05m,
                    0.01m,
                    minProfit,
                    idle);

            var configs = new List<TradeSessionConfig>();

            const decimal profitStep = 0.1m;
            const decimal idleStep = 0.5m;

            var profit = 0.1m;
            while (profit <= 2)
            {
                var idle = 0.5m;
                while (idle <= 12m)
                {
                    configs.Add(CreateConfig(profit, idle));
                    idle += idleStep;
                }

                profit += profitStep;
            }

            //configs.Add(CreateConfig(1, 1));

            return configs;
        }

        private static void PreventAppClose()
        {
            Task.Delay(-1).NotNull().Wait();
        }
    }
}