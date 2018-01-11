using System;
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
                "ADA",
                "ETH",
                new DateTime(2018, 01, 11, 15, 0, 0),
                //now,
                new DateTime(2018, 01, 11, 18, 0, 0),
                CandlesInterval.Minutes1);

            var macd = candles.CalculateMACD(12, 26, 9);

            Console.WriteLine();
            Console.WriteLine("--------------");
            Console.WriteLine("Basic");
            Console.WriteLine("--------------");
            Console.WriteLine();

            var profit1= SimulateTrade(macd, new BasicTradeStrategy());

            Console.WriteLine();
            Console.WriteLine("--------------");
            Console.WriteLine("Advanced");
            Console.WriteLine("--------------");
            Console.WriteLine();

            var profit2= SimulateTrade(macd, new AdvancedTradeStrategy());

            Console.WriteLine();
            Console.WriteLine("--------------");
            Console.WriteLine("EMA");
            Console.WriteLine("--------------");
            Console.WriteLine();

            var profit3= SimulateTrade(macd, new EMATradeStrategy());
        }

        private static decimal SimulateTrade(List<MACDItem> macd, ITradeStrategy strategy)
        {
            const decimal fluctuation = 0.2m;
            const decimal fee = 0.1m;
            const decimal minQuoteAmount = 0.01m;

            var account = new MockTradingAccount(0, 1, 0, fee);
            var nextAction = TradeAction.Buy;

            for (var i = 0; i < macd.Count; i++)
            {
                var item = macd[i].NotNull();

                if (i == 0)
                {
                    continue;
                }

                var range = macd.GetRange(0, i + 1);
                var action = strategy.DefineTradeAction(range);
                var price = item.Candle.NotNull().ClosePrice;

                if (nextAction == TradeAction.Buy && action == TradeAction.Buy)
                {
                    var baseAmount = Math.Floor(account.CurrentQuoteAmount / price);

                    if (account.CurrentQuoteAmount > minQuoteAmount && baseAmount > 0
                        //&& (account.LastPrice == 0 || price < account.LastPrice)
                    )
                    {
                        account.Buy(baseAmount, price);
                        nextAction = TradeAction.Sell;

                        LogOrder("Buy", account, item);
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

                        LogOrder("Sell", account, item);
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

        private static void LogOrder(string action, [NotNull] ITradingAccount ta, [NotNull] MACDItem macdItem)
        {
            var ca = ta.CurrentBaseAmount * ta.LastPrice + ta.CurrentQuoteAmount;

            Console.WriteLine(action);
            //Console.WriteLine($"Trend :{point.Type}");
            Console.WriteLine(macdItem.Candle.OpenTime);
            Console.WriteLine($"Price: {ta.LastPrice}");
            Console.WriteLine($"Base amount: {ta.CurrentBaseAmount}");
            Console.WriteLine($"Total: {ca.Round()}");
            Console.WriteLine();
        }
    }
}