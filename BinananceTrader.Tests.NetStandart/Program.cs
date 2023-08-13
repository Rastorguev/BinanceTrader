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

            var connectionStringsProvider = new ConnectionStringsProvider();
            var connectionString = connectionStringsProvider.GetConnectionString("BlobStorage");

            const string traderName = "Google";
            var keys = new BlobKeyProvider(connectionString).GetKeys().First(x => x.NotNull().Name == traderName);
            var apiClient = new ApiClient(keys.NotNull().Api, keys.NotNull().Secret);
            var client = new BinanceClient(apiClient);
            var candlesProvider = new CandlesProvider(client);
            var rules = client.LoadTradingRules().Result.NotNull();
            var logger = new Logger(traderName);

            const string quoteAsset = "ETH";
            var assets = rules.Rules.NotNull()
                .Where(r => r.NotNull().QuoteAsset == quoteAsset)
                .Select(r => r.BaseAsset)
                .OrderBy(a => a)
                .ToList();

            var configs = GetConfigs();
            var tests = new StrategiesTests(candlesProvider);
            var watch = Stopwatch.StartNew();

            var candles = tests.LoadCandles(
                    assets,
                    quoteAsset,
                    new DateTime(2019, 11, 07, 00, 00, 00),
                    new DateTime(2019, 11, 10, 23, 00, 00),
                    TimeInterval.Minutes_1)
                .Result
                .NotNull();

            var results = tests.CompareStrategies(candles, configs);

            watch.Stop();
            var elapsed = new TimeSpan(watch.ElapsedTicks);

            var ordered = results.NotNull().OrderByDescending(r => r.Value.NotNull().TradeProfit).ToList();
            var max = ordered.First();
            var min = ordered.Last();

            Console.WriteLine($"Elapsed Time: {elapsed.TotalSeconds}");

            var tradeResults = ordered.Select(o => (
                    Profit: o.Key.NotNull().ProfitRatio,
                    Idle: o.Key.NotNull().MaxIdlePeriod,
                    o.Value.NotNull().TradeProfit,
                    TradesCount: o.Value.NotNull().CompletedCount))
                .OrderByDescending(o => o.TradeProfit)
                .ToList();

            Debugger.Break();

            PreventAppClose();
        }

        [NotNull]
        [ItemNotNull]
        private static IReadOnlyList<TradeSessionConfig> GetConfigs()
        {
            TradeSessionConfig CreateConfig(decimal minProfit, TimeSpan idle)
            {
                return new TradeSessionConfig(
                    1m,
                    0.075m,
                    0.01m,
                    minProfit,
                    idle);
            }

            var configs = new List<TradeSessionConfig>();

            var startProfit = 0.5m;
            var startIdle = TimeSpan.FromMinutes(1);

            var maxProfit = 4m;
            var maxIdle = TimeSpan.FromMinutes(10);

            var profitStep = 0.5m;
            var idleStep = TimeSpan.FromMinutes(1);

            configs.Add(CreateConfig(0.5m, TimeSpan.FromDays(365)));
           
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

            startProfit = 0.5m;
            startIdle = TimeSpan.FromHours(0.5);

            maxProfit = 4m;
            maxIdle = TimeSpan.FromHours(12);

            profitStep = 0.5m;
            idleStep = TimeSpan.FromHours(0.5);

            profit = startProfit;
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

            return configs;
        }

        private static void PreventAppClose()
        {
            Task.Delay(-1).NotNull().Wait();
        }
    }
}