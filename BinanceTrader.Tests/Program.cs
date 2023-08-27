using System.Diagnostics;
using System.Globalization;
using System.Net;
using BinanceApi.Client;
using BinanceApi.Domain.Interfaces;
using BinanceApi.Models.Account;
using BinanceApi.Models.Enums;
using BinanceApi.Models.Extensions;
using BinanceApi.Models.Market.TradingRules;
using BinanceTrader.Core;
using BinanceTrader.Core.Analysis;
using BinanceTrader.Core.TradeHistory;
using BinanceTrader.Tools.KeyProviders;

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

        var assets = rules.Rules
            .Where(r => r.QuoteAsset == QuoteAsset && r.Status == SymbolStatus.Trading)
            .Select(r => r.BaseAsset)
            .OrderBy(a => a)
            .ToList();

        //await AnaliseTradesHistory(assets, QuoteAsset, client);

        var configs = GetConfigs();
        var watch = Stopwatch.StartNew();

        var candles = await candlesProvider.LoadCandles(
            assets,
            QuoteAsset,
            //Current Period
            // new DateTime(2023, 08, 01, 00, 00, 00),
            // new DateTime(2023, 08, 27, 00, 00, 00),

            //From the biginning of 2023
            new DateTime(2023, 01, 01, 00, 00, 00),
            new DateTime(2023, 08, 27, 00, 00, 00),

            //Bull Run 2017
            // new DateTime(2017, 11, 01, 00, 00, 00),
            // new DateTime(2018, 02, 01, 00, 00, 00),

            //Bull Run 2021
            // new DateTime(2021, 01, 01, 00, 00, 00),
            // new DateTime(2021, 12, 01, 00, 00, 00),
            TimeInterval.Minutes_1);

        candles = candles
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

        var ordered = results
            .OrderByDescending(r => r.Value.TradeProfitPercentage)
            .ToList();

        var max = ordered.First();
        var min = ordered.Last();

        Console.WriteLine($"Elapsed Time: {elapsed.TotalSeconds}");

        var tradeResults = ordered.Select(o => (
                Profit: o.Key.ProfitRatio,
                Idle: o.Key.MaxIdlePeriod,
                TradeProfitPercentages: o.Value.TradeProfitPercentage,
                TradesCount: o.Value.CompletedCount))
            .OrderByDescending(o => o.TradeProfitPercentages)
            .ToList();

        Debugger.Break();
    }

    private static async Task AnaliseTradesHistory(IReadOnlyList<string> baseAssets, string quoteAsset,
        IBinanceClient client)
    {
        var startTime = new DateTime(2023, 08, 01, 00, 00, 00);
        var endTime = DateTime.Now; //new DateTime(2019, 05, 01, 00, 00, 00);

        var tradeHistoryLoader = new TradeHistoryLoader(client);
        var tradeHistory = await tradeHistoryLoader.LoadTradeHistory(baseAssets, quoteAsset, startTime, endTime);

        var analysis = TechAnalyzer.AnalyzeTradeHistory(tradeHistory, 0.1289m);
    }

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
        Task.Delay(-1).Wait();
    }
}