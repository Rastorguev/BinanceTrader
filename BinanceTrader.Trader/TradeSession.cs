using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Core.Entities.Enums;
using BinanceTrader.Tools;
using JetBrains.Annotations;
using Trady.Core;
using Trady.Core.Infrastructure;

namespace BinanceTrader
{
    public class TradeSession
    {
        [NotNull] private readonly TradeSessionConfig _config;
        [NotNull] private readonly Predicate<IIndexedOhlcv> _buyRule;
        [NotNull] private readonly Predicate<IIndexedOhlcv> _sellRule;

        public TradeSession(
            [NotNull] TradeSessionConfig config,
            [NotNull] Predicate<IIndexedOhlcv> buyRule,
            [NotNull] Predicate<IIndexedOhlcv> sellRule)
        {
            _config = config;
            _buyRule = buyRule;
            _sellRule = sellRule;
        }

        [NotNull]
        public ITradeAccount Run(IEnumerable<Candle> candles)
        {
            var account = new MockTradeAccount(0, _config.InitialQuoteAmount, _config.InitialPrice, _config.Fee);
            candles = candles.OrderBy(c => c.DateTime).ToList();
            //var tradeActions = DefineTradeActions(candles).NotNull();

            var nextAction = TradeActionType.Buy;
            //var prevPrice = _config.InitialPrice;

            DateTime? lastActionDate = null;


            if (!candles.Any())
            {
                return account;
            }


            var nextPrice = candles.First().Close;

            foreach (var candle in candles)
            {
                //var price = candle.Close;

                var
                    force = lastActionDate == null ||
                            candle.DateTime - lastActionDate.Value >= TimeSpan.FromHours(4);
                //var force = false;

                var fluctuation = 2;

                if (nextAction == TradeActionType.Buy &&
                    (nextPrice >= candle.Low && nextPrice <= candle.High || force))
                {
                    var price = nextPrice; //account.LastPrice - account.LastPrice.Percents(_config.MinProfitRatio);


                    if (force)
                    {
                        //if (Math.Abs(candle.Close - price) < price.Percents(1))
                        //{
                            price = candle.Close;
                        //}
                        //else
                        //{
                        //    continue;
                        //}
                    }

                    var estimatedBaseAmount = Math.Floor(account.CurrentQuoteAmount / price);
                    if (account.CurrentQuoteAmount > _config.MinQuoteAmount && estimatedBaseAmount > 0)
                    {
                        account.Buy(estimatedBaseAmount, price, candle.DateTime.DateTime);

                        //Console.WriteLine(nextAction);
                        //Console.WriteLine(candle.DateTime);
                        //Console.WriteLine(price);
                        //Console.WriteLine(account.GetProfit());
                        //Console.WriteLine(force);
                        //Console.WriteLine();
                        nextPrice = price + price.Percents(fluctuation);
                        nextAction = TradeActionType.Sell;
                        lastActionDate = candle.DateTime.DateTime;
                    }
                }
                else if (nextAction == TradeActionType.Sell && nextPrice >= candle.Low && nextPrice <= candle.High || force)
                {
                    var price = nextPrice; //account.LastPrice + account.LastPrice.Percents(_config.MinProfitRatio);

                    if (force)
                    {
                        //if (Math.Abs(candle.Close - price) < price.Percents(2))
                        //{
                            price = candle.Close;
                        //}
                        //else
                        //{
                        //    continue;
                        //}
                    }

                    var baseAmount = Math.Floor(account.CurrentBaseAmount);
                    if (baseAmount > 0)
                    {
                        account.Sell(baseAmount, price, candle.DateTime.DateTime);

                        //Console.WriteLine(nextAction);
                        //Console.WriteLine(candle.DateTime);
                        //Console.WriteLine(price);
                        //Console.WriteLine(account.GetProfit());
                        //Console.WriteLine(force);
                        //Console.WriteLine();
                        nextPrice = price - price.Percents(fluctuation);
                        nextAction = TradeActionType.Buy;
                        lastActionDate = candle.DateTime.DateTime;
                    }
                }
            }

            return account;
        }

        //private List<(IOhlcv Candle, TradeActionType Type)> DefineTradeActions(IEnumerable<Candle> candles)
        //{
        //    var actionCandles = new List<(IOhlcv, TradeActionType)>();

        //    using (var ctx = new AnalyzeContext(candles))
        //    {
        //        var buyActions = new SimpleRuleExecutor(ctx, _buyRule).Execute().NotNull()
        //            .Select(c => ((IOhlcv)c, TradeActionType.Buy));

        //        var sellCandles = new SimpleRuleExecutor(ctx, _sellRule).Execute().NotNull()
        //            .Select(c => ((IOhlcv)c, TradeActionType.Sell));

        //        actionCandles.AddRange(buyActions);
        //        actionCandles.AddRange(sellCandles);
        //        actionCandles = actionCandles.OrderBy(c => c.Item1.NotNull().DateTime).ToList();
        //    }

        //    return actionCandles;
        //}
    }

    public class TradeSessionConfig
    {
        public decimal InitialQuoteAmount { get; }
        public decimal InitialPrice { get; }
        public decimal Fee { get; }
        public decimal MinQuoteAmount { get; }
        public decimal MinProfitRatio { get; }

        public TradeSessionConfig(
            decimal initialQuoteAmount,
            decimal initialPrice,
            decimal fee,
            decimal minQuoteAmount,
            decimal minProfitRatio)
        {
            InitialQuoteAmount = initialQuoteAmount;
            Fee = fee;
            MinQuoteAmount = minQuoteAmount;
            MinProfitRatio = minProfitRatio;
            InitialPrice = initialPrice;
        }
    }
}