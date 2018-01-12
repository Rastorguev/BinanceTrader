using System.Collections.Generic;
using BinanceTrader.Indicators;

namespace BinanceTrader.TradeStrategies
{
    public class SMACrossingTradeStrategy : ITradeStrategy
    {
        private readonly int _shortSMAPeriod;
        private readonly int _longSMAPeriod;

        public SMACrossingTradeStrategy(int shortEMAPeriod, int longEMAPeriod)
        {
            _shortSMAPeriod = shortEMAPeriod;
            _longSMAPeriod = longEMAPeriod;
        }

        public TradeAction GetTradeAction(List<decimal> prices)
        {
            var shortSMA = SMA.Calculate(prices, _shortSMAPeriod);
            var longSMA = SMA.Calculate(prices, _longSMAPeriod);

            if (IndicatorsUtils.CrossesFromBelow(shortSMA, longSMA))
            {
                return TradeAction.Buy;
            }

            if (IndicatorsUtils.CrossesFromAbove(shortSMA, longSMA))
            {
                return TradeAction.Sell;
            }

            return TradeAction.Ignore;
        }
    }
}