using System.Collections.Generic;
using BinanceTrader.Indicators;

namespace BinanceTrader.TradeStrategies
{
    public class EMACrossingTradeStrategy : ITradeStrategy
    {
        private readonly int _shortEMAPeriod;
        private readonly int _longEMAPeriod;

        public EMACrossingTradeStrategy(int shortEMAPeriod, int longEMAPeriod)
        {
            _shortEMAPeriod = shortEMAPeriod;
            _longEMAPeriod = longEMAPeriod;
        }

        public TradeAction GetTradeAction(List<decimal> prices)
        {
            var shortEMA = EMA.Calculate(prices, _shortEMAPeriod);
            var longEMA = EMA.Calculate(prices, _longEMAPeriod);

            if (IndicatorsUtils.CrossesFromBelow(shortEMA, longEMA))
            {
                return TradeAction.Buy;
            }

            if (IndicatorsUtils.CrossesFromAbove(shortEMA, longEMA))
            {
                return TradeAction.Sell;
            }

            return TradeAction.Ignore;
        }
    }
}