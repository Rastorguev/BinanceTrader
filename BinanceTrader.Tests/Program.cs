﻿using System.Diagnostics;
using System.Globalization;
using System.Net;
using BinanceApi.Client;
using BinanceApi.Models.Enums;
using BinanceApi.Models.Extensions;
using BinanceApi.Models.Market.TradingRules;
using BinanceTrader.Core;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Program
{
    private static void Main()
    {
        ServicePointManager.DefaultConnectionLimit = 10;

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var apiClient = new ApiClient(string.Empty, string.Empty);
        var client = new BinanceClient(apiClient);
        var candlesProvider = new CandlesProvider(client);
        var rules = client.LoadTradingRules().Result.NotNull();

        const string quoteAsset = "ETH";
        var assets = rules.Rules.NotNull()
            .Where(r => r.NotNull().QuoteAsset == quoteAsset && r.NotNull().Status == SymbolStatus.Trading)
            .Select(r => r.BaseAsset)
            .OrderBy(a => a)
            .ToList();

        var configs = GetConfigs();
        var tests = new StrategiesTests(candlesProvider);
        var watch = Stopwatch.StartNew();

        var candles = tests.LoadCandles(
                assets,
                quoteAsset,
                //Current Period
                // new DateTime(2023, 08, 01, 00, 00, 00),
                // new DateTime(2023, 08, 18, 00, 00, 00),

                //Bull Run 2017
                // new DateTime(2017, 11, 01, 00, 00, 00),
                // new DateTime(2018, 02, 01, 00, 00, 00),

                //Bull Run 2021
                new DateTime(2021, 01, 01, 00, 00, 00),
                new DateTime(2021, 12, 01, 00, 00, 00),
                TimeInterval.Minutes_1)
            .Result
            .NotNull()
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

        var results = tests.CompareStrategies(candles, configs);

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

        PreventAppClose();
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