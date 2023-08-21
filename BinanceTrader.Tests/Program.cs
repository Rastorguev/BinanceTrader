using System.Diagnostics;
using System.Globalization;
using System.Net;
using BinanceApi.Client;
using BinanceApi.Models.Account;
using BinanceApi.Models.Enums;
using BinanceApi.Models.Extensions;
using BinanceApi.Models.Market.TradingRules;
using BinanceTrader.Core;
using BinanceTrader.Tools;
using BinanceTrader.Tools.KeyProviders;
using JetBrains.Annotations;

namespace BinanceTrader.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Program
{
    private const string TraderName = "Rambler";
    const string QuoteAsset = "ETH";

    private static void Main()
    {
        RunTests().Wait();
        PreventAppClose();
    }

    private static async Task RunTests()
    {
        ServicePointManager.DefaultConnectionLimit = 10;

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var connectionStringsProvider = new ConnectionStringsProvider();
        var connectionString = connectionStringsProvider.GetConnectionString("BlobStorage");
        var keys = (await new BlobKeyProvider(connectionString).GetKeysAsync()).First(x => x.Name == TraderName);

        var apiClient = new ApiClient(
            keys.Api,
            keys.Secret);

        var client = new BinanceClient(apiClient);
        var candlesProvider = new CandlesProvider(client);
        var rules = await client.LoadTradingRules();

        var assets = rules.Rules.NotNull()
            .Where(r => r.NotNull().QuoteAsset == QuoteAsset && r.NotNull().Status == SymbolStatus.Trading)
            .Select(r => r.BaseAsset)
            .OrderBy(a => a)
            .ToList();

        var assetsTradesHistory = new Dictionary<string, IReadOnlyList<Trade>>();
        var historyStartTime = new DateTime(2023, 08, 01, 00, 00, 00);
        //var historyEndTime = new DateTime(2019, 05, 01, 00, 00, 00);

        // foreach (var asset in assets)
        // {
        //     var symbol = SymbolUtils.GetCurrencySymbol(asset, QuoteAsset);
        //
        //     Console.WriteLine($"Trade History Load Started: {symbol}");
        //
        //     var tradeHistory = (await client.GetTradeList(symbol, historyStartTime)).ToList();
        //     await Task.Delay(300);
        //
        //     Console.WriteLine($"Trade History Load Finished: {symbol}");
        //
        //     assetsTradesHistory.Add(asset, tradeHistory);
        // }
        //
        // var analysis = TechAnalyzer.AnalyzeTradeHistory(assetsTradesHistory, 0.1289m);

        var configs = GetConfigs();
        var tests = new StrategiesTests(candlesProvider);
        var watch = Stopwatch.StartNew();

        var candles = (await tests.LoadCandles(
                assets,
                QuoteAsset,
                //Current Period
                // new DateTime(2023, 08, 01, 00, 00, 00),
                // new DateTime(2023, 08, 18, 00, 00, 00),

                //Bull Run 2017
                new DateTime(2017, 11, 01, 00, 00, 00),
                new DateTime(2018, 02, 01, 00, 00, 00),

                //Bull Run 2021
                // new DateTime(2021, 01, 01, 00, 00, 00),
                // new DateTime(2021, 12, 01, 00, 00, 00),
                TimeInterval.Minutes_1))
            .Where(x => x.Value.Any())
            .ToDictionary(x => x.Key, x => x.Value);

        var volatility = candles
            .Select(x => (BaseAsset: x.Key, Volatility: TechAnalyzer.CalculateVolatilityIndex(x.Value)))
            .OrderByDescending(x => x.Volatility)
            .ToList();

        var medianVolatility = volatility.Select(v => v.Volatility).Median();
        var averageVolatility = volatility.Select(v => v.Volatility).Average();

        var n = 10;
        var mostVolatile = volatility.Take(n);
        var lessVolatile = volatility.Skip(Math.Max(0, volatility.Count - n));

        var tradeAssets = mostVolatile
            .Select(x => x.BaseAsset)
            .ToList();

        var tradeAssetsCandles = candles
            .Where(x => tradeAssets.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value);

        var results = StrategiesTests.CompareStrategies(candles, configs);

        watch.Stop();
        var elapsed = new TimeSpan(watch.ElapsedTicks);

        var ordered = results.NotNull()
            .OrderByDescending(r => r.Value.NotNull().TradeProfit)
            .ToList();

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
    }

    [NotNull]
    [ItemNotNull]
    private static IReadOnlyList<TradeSessionConfig> GetConfigs()
    {
        var configs = new List<TradeSessionConfig>();

        AddNormalProfitConfigs(configs);
        AddSmallProfitConfigs(configs);

        return configs;
    }

    private static TradeSessionConfig CreateConfig(decimal minProfit, TimeSpan idle)
    {
        return new TradeSessionConfig(
            1m,
            0.075m,
            1m,
            0.01m,
            minProfit,
            idle);
    }

    private static void AddNormalProfitConfigs(ICollection<TradeSessionConfig> configs)
    {
        const decimal startProfit = 0.5m;
        const decimal maxProfit = 4m;
        const decimal profitStep = 0.5m;

        var startIdle = TimeSpan.FromHours(0.5);
        var maxIdle = TimeSpan.FromHours(12);
        var idleStep = TimeSpan.FromHours(0.5);

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
    }

    private static void AddSmallProfitConfigs(ICollection<TradeSessionConfig> configs)
    {
        const decimal startProfit = 0.1m;
        const decimal maxProfit = 1m;
        const decimal profitStep = 0.1m;

        var startIdle = TimeSpan.FromMinutes(1);
        var maxIdle = TimeSpan.FromMinutes(10);
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
    }

    private static void PreventAppClose()
    {
        Task.Delay(-1).NotNull().Wait();
    }
}