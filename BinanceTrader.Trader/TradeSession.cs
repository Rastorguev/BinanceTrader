using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Core.Entities.Enums;
using BinanceTrader.Tools;
using JetBrains.Annotations;
using Trady.Analysis;
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
            foreach (var candle in candles)
            {
                var price = candle.Close;
                
                if (nextAction == TradeActionType.Buy )
                {
                    var estimatedBaseAmount = Math.Floor(account.CurrentQuoteAmount / price);
                    if (account.CurrentQuoteAmount > _config.MinQuoteAmount && estimatedBaseAmount > 0
                        && price + price.Percents(_config.MinProfitRatio) <= account.LastPrice)
                    {
                        account.Buy(estimatedBaseAmount, price, candle.DateTime.DateTime);
                        nextAction = TradeActionType.Sell;
                    }
                }
                else if (nextAction == TradeActionType.Sell)
                {
                    var baseAmount = Math.Floor(account.CurrentBaseAmount);

                    if (baseAmount > 0
                        && price > account.LastPrice + account.LastPrice.Percents(_config.MinProfitRatio))
                    {
                        account.Sell(baseAmount, price, candle.DateTime.DateTime);
                        nextAction = TradeActionType.Buy;
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