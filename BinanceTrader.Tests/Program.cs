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
using BinanceTrader.Tools.KeyProviders;
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

            var keyProvider = new ConfigFileKeyProvider();
            var keys = keyProvider.GetKeys("Rambler").NotNull();
            var apiClient = new ApiClient(keys.Api, keys.Secret);
            var binanceClient = new BinanceClient(apiClient);
            var candlesProvider = new CandlesProvider(binanceClient);

            var rules = binanceClient.LoadTradingRules().Result;
            var assets = rules.Rules.Where(r => r.NotNull().QuoteAsset == "ETH").Select(r => r.BaseAsset).ToList();
            //var assets = AssetsProvider.Assets;

            var configs = GetConfigs();
            var tests = new StrategiesTests(candlesProvider);

            var watch = Stopwatch.StartNew();

            var results = tests.CompareStrategies(assets,
                    "ETH",
                    //new DateTime(2017, 09, 01, 0, 0, 0),
                    //new DateTime(2018, 06, 25, 9, 0, 0),
                    //new DateTime(2018, 03, 01, 0, 0, 0),
                    //new DateTime(2018, 06, 01, 0, 0, 0),

                    new DateTime(2018, 05, 1, 0, 0, 0),
                    new DateTime(2018, 06, 28, 0, 0, 0),
                    TimeInterval.Minutes_1,
                    configs)
                .Result;

            watch.Stop();
            var elapsed = new TimeSpan(watch.ElapsedTicks);

            var ordered = results.NotNull().OrderByDescending(r => r.Value.NotNull().TradeProfit).ToList();
            var max = ordered.First();
            var min = ordered.Last();

            var x1_1 = ordered.First(r =>
              r.Key.NotNull().ProfitRatio == 1 && r.Key.NotNull().MaxIdlePeriod == TimeSpan.FromMinutes(60));

            var x1_01 = ordered.First(r =>
                r.Key.NotNull().ProfitRatio == 1m && r.Key.NotNull().MaxIdlePeriod == TimeSpan.FromMinutes(1));

            var x1_02 = ordered.First(r =>
                r.Key.NotNull().ProfitRatio == 1m && r.Key.NotNull().MaxIdlePeriod == TimeSpan.FromMinutes(2));

            var x1_03 = ordered.First(r =>
                r.Key.NotNull().ProfitRatio == 1m && r.Key.NotNull().MaxIdlePeriod == TimeSpan.FromMinutes(3));

            var x1_04 = ordered.First(r =>
                r.Key.NotNull().ProfitRatio == 1m && r.Key.NotNull().MaxIdlePeriod == TimeSpan.FromMinutes(4));

            var x1_05 = ordered.First(r =>
                r.Key.NotNull().ProfitRatio == 1m && r.Key.NotNull().MaxIdlePeriod == TimeSpan.FromMinutes(5));

            var x1_10 = ordered.First(r =>
                r.Key.NotNull().ProfitRatio == 1m && r.Key.NotNull().MaxIdlePeriod == TimeSpan.FromMinutes(10));

            var i1_1 = ordered.IndexOf(x1_1);
            var i01_1 = ordered.IndexOf(x1_01);
            var i01_2 = ordered.IndexOf(x1_02);
            var i01_3 = ordered.IndexOf(x1_03);
            var i01_4 = ordered.IndexOf(x1_04);
            var i01_5 = ordered.IndexOf(x1_05);
            var i01_10 = ordered.IndexOf(x1_10);

            Console.WriteLine($"Elapsed Time: {elapsed.TotalSeconds}");

            Debugger.Break();

            PreventAppClose();
        }

        [NotNull]
        [ItemNotNull]
        private static IReadOnlyList<TradeSessionConfig> GetConfigs()
        {
            TradeSessionConfig CreateConfig(decimal minProfit, TimeSpan idle) =>
                new TradeSessionConfig(
                    1m,
                    0.05m,
                    0.01m,
                    minProfit,
                    idle);

            var configs = new List<TradeSessionConfig>();

            const decimal startProfit = 1m;
            var startIdle = TimeSpan.FromMinutes(1);

            const decimal maxProfit = 1m;
            var maxIdle = TimeSpan.FromMinutes(10);

            const decimal profitStep = 1m;
            var idleStep = TimeSpan.FromMinutes(1);

            var profit = startProfit;
            while (profit <= maxProfit)
            {
                var idle = startIdle;
                while (idle <= maxIdle)
                {
                    configs.Add(CreateConfig(profit, idle));
                    idle += idleStep;
                }

                profit += profitStep;
            }

            configs.Add(CreateConfig(1, TimeSpan.FromHours(1)));


            return configs;
        }

        private static void PreventAppClose()
        {
            Task.Delay(-1).NotNull().Wait();
        }
    }
}