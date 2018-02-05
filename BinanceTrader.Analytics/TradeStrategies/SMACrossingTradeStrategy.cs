using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Analytics.Indicators;
using BinanceTrader.Core.Entities;
using BinanceTrader.Core.Entities.Enums;

namespace BinanceTrader.Analytics.TradeStrategies
{
    public class SMACrossingTradeStrategy : ITradeStrategy
    {
        private readonly int _shortSMAPeriod;
        private readonly int _longSMAPeriod;

        public SMACrossingTradeStrategy(int shortSMAPeriod, int longSMAPeriod)
        {
            _shortSMAPeriod = shortSMAPeriod;
            _longSMAPeriod = longSMAPeriod;
        }

        public TradeActionType GetTradeAction(List<Candle> candles)
        {
            var prices = candles.Select(c => c.ClosePrice).ToList();

            if (prices.Count < _longSMAPeriod)
            {
                return TradeActionType.Ignore;
            }

            var shortSMA = SMA.Calculate(prices, _shortSMAPeriod);
            var longSMA = SMA.Calculate(prices, _longSMAPeriod);

            if (IndicatorsUtils.CrossesFromBelow(shortSMA, longSMA))
            {
                return TradeActionType.Buy;
            }

            if (IndicatorsUtils.CrossesFromAbove(shortSMA, longSMA))
            {
                return TradeActionType.Sell;
            }

            return TradeActionType.Ignore;
        }
    }
}