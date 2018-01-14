﻿using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Api;
using BinanceTrader.Entities;
using BinanceTrader.TradeStrategies;
using BinanceTrader.Utils;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public class PriceMonitor
    {
        [NotNull] private readonly BinanceApi _api;

        public PriceMonitor(
            [NotNull] BinanceApi api
        )
        {
            _api = api;
        }

        public void Start()
        {
            var now = DateTime.Now;
            var candles = LoadCandles(
                "TRX",
                "ETH",
                new DateTime(2018, 1, 1, 0, 0, 0),
                //now,
                new DateTime(2018, 1, 14, 12, 0, 0),
                CandlesInterval.Minutes1);

            const int shortEMAPeriod = 7;
            const int longEMAPeriod = 25;
            const int signalPeriod = 9;

            Console.WriteLine();
            Console.WriteLine("--------------");
            Console.WriteLine("MACDHist");
            Console.WriteLine("--------------");
            Console.WriteLine();

            var profit1 = SimulateTrade(candles, new MACDHistStrategy(shortEMAPeriod, longEMAPeriod, signalPeriod));

            //Console.WriteLine();
            //Console.WriteLine("--------------");
            //Console.WriteLine("OneMinScalping");
            //Console.WriteLine("--------------");
            //Console.WriteLine();

            //var s = (int)Math.Floor((longEMAPeriod + shortEMAPeriod) / 2m);
            //var profit2 = SimulateTrade(prices, new OneMinScalpingStrategy(shortEMAPeriod, longEMAPeriod, s + 1));

            Console.WriteLine();
            Console.WriteLine("--------------");
            Console.WriteLine("EMA");
            Console.WriteLine("--------------");
            Console.WriteLine();

            var profit3 = SimulateTrade(candles, new EMACrossingTradeStrategy(shortEMAPeriod, longEMAPeriod));

            Console.WriteLine();
            Console.WriteLine("--------------");
            Console.WriteLine("SMA");
            Console.WriteLine("--------------");
            Console.WriteLine();

            var profit4 = SimulateTrade(candles, new SMACrossingTradeStrategy(shortEMAPeriod, longEMAPeriod));
        }

        private static decimal SimulateTrade(List<Candle> candles, ITradeStrategy strategy)
        {
            //var prices = candles.Select(c => c.ClosePrice).ToList();

            const decimal fluctuation = 0.2m;
            const decimal fee = 0.1m;
            const decimal minQuoteAmount = 0.01m;

            var account = new MockTradingAccount(0, 1, 0, fee);
            var nextAction = TradeAction.Buy;

            for (var i = 0; i < candles.Count; i++)
            {
                if (i == 0)
                {
                    continue;
                }
                var price = candles[i].ClosePrice;
                const int max = 100;
                var start = i < max ? 0 : i - max;
                var range = candles.GetRange(start, i - start + 1);

                if (range.Count > 200)
                {
                    range = range.GetRange(range.Count - 200 - 1, 200);
                }
                var action = strategy.GetTradeAction(range);

                if (nextAction == TradeAction.Buy && action == TradeAction.Buy)
                {
                    var baseAmount = Math.Floor(account.CurrentQuoteAmount / price);

                    if (account.CurrentQuoteAmount > minQuoteAmount && baseAmount > 0
                        //&& (account.LastPrice == 0 || price + fee <= account.LastPrice)
                    )
                    {
                        account.Buy(baseAmount, price);
                        nextAction = TradeAction.Sell;

                        LogOrder("Buy", account, price, candles[i].OpenTime);
                    }
                }
                else if (nextAction == TradeAction.Sell && action == TradeAction.Sell)
                {
                    var baseAmount = Math.Floor(account.CurrentBaseAmount);

                    if (baseAmount > 0 &&
                        price > account.LastPrice + account.LastPrice.Percents(fluctuation))
                    {
                        account.Sell(baseAmount, price);
                        nextAction = TradeAction.Buy;

                        LogOrder("Sell", account, price, candles[i].OpenTime);
                    }
                }
            }

            var initialAmount = account.InitialBaseAmount * account.InitialPrice + account.InitialQuoteAmount;
            var currentAmount = account.CurrentBaseAmount * account.LastPrice + account.CurrentQuoteAmount;
            var profit = MathUtils.CalculateProfit(
                initialAmount,
                currentAmount).Round();

            Console.WriteLine($"Profit {profit}");

            return profit;
        }

        [NotNull]
        private List<Candle> LoadCandles(string baseAsset, string quoteAsset, DateTime start, DateTime end,
            CandlesInterval interval)
        {
            const int maxRange = 500;
            var candles = new List<Candle>();

            while (start < end)
            {
                var intervalMinutes = maxRange * interval.ToMinutes();
                var rangeEnd = (end - start).TotalMinutes > intervalMinutes
                    ? start.AddMinutes(intervalMinutes)
                    : end;

                var rangeCandles = _api.GetCandles(baseAsset, quoteAsset, interval, start, rangeEnd).NotNull()
                    .Result.NotNull()
                    .ToList();

                candles.AddRange(rangeCandles);
                start = rangeEnd;
            }

            return candles.OrderBy(c => c.NotNull().OpenTime).ToList();
        }

        private static void LogOrder(string action, [NotNull] ITradingAccount ta, decimal price, DateTime openTime)
        {
            var ca = ta.CurrentBaseAmount * ta.LastPrice + ta.CurrentQuoteAmount;

            Console.WriteLine(action);
            Console.WriteLine(openTime);
            //Console.WriteLine($"Trend :{point.Type}");
            Console.WriteLine(price);
            Console.WriteLine($"Price: {ta.LastPrice}");
            Console.WriteLine($"Base amount: {ta.CurrentBaseAmount}");
            Console.WriteLine($"Total: {ca.Round()}");
            Console.WriteLine();
        }
    }
}