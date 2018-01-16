using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Utils;
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

        public ITradeAccount Run(IEnumerable<Candle> candles)
        {
            var account = new MockTradeAccount(0, _config.InitialQuoteAmount, 0, _config.Fee);
            var tradeActions = DefineTradeActions(candles).NotNull();

            var nextAction = TradeActionType.Buy;
            foreach (var action in tradeActions)
            {
                var price = action.Candle.Close;
                var candle = action.Candle;

                if (nextAction == TradeActionType.Buy &&
                    action.Type == TradeActionType.Buy)
                {
                    var baseAmount = Math.Floor(account.CurrentQuoteAmount / price);
                    if (account.CurrentQuoteAmount > _config.MinQuoteAmount && baseAmount > 0)
                    {
                        account.Buy(baseAmount, price, candle.DateTime.DateTime);
                        nextAction = TradeActionType.Sell;
                    }
                }
                else if (nextAction == TradeActionType.Sell && action.Type == TradeActionType.Sell)
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

        private List<(IOhlcv Candle, TradeActionType Type)> DefineTradeActions(IEnumerable<Candle> candles)
        {
            var actionCandles = new List<(IOhlcv, TradeActionType)>();

            using (var ctx = new AnalyzeContext(candles))
            {
                var buyActions = new SimpleRuleExecutor(ctx, _buyRule).Execute().NotNull()
                    .Select(c => ((IOhlcv) c, TradeActionType.Buy));

                var sellCandles = new SimpleRuleExecutor(ctx, _sellRule).Execute().NotNull()
                    .Select(c => ((IOhlcv) c, TradeActionType.Sell));

                actionCandles.AddRange(buyActions);
                actionCandles.AddRange(sellCandles);
                actionCandles = actionCandles.OrderBy(c => c.Item1.NotNull().DateTime).ToList();
            }

            return actionCandles;
        }
    }

    public class TradeSessionConfig
    {
        public decimal InitialQuoteAmount { get; }
        public decimal Fee { get; }
        public decimal MinQuoteAmount { get; }
        public decimal MinProfitRatio { get; }

        public TradeSessionConfig(
            decimal initialQuoteAmount,
            decimal fee,
            decimal minQuoteAmount,
            decimal minProfitRatio)
        {
            InitialQuoteAmount = initialQuoteAmount;
            Fee = fee;
            MinQuoteAmount = minQuoteAmount;
            MinProfitRatio = minProfitRatio;
        }
    }
}