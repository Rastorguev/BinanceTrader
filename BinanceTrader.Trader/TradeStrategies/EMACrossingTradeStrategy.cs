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

            if (CrossesFromBelow(shortEMA, longEMA))
            {
                return TradeAction.Buy;
            }

            if (CrossesFromAbove(shortEMA, longEMA))
            {
                return TradeAction.Sell;
            }

            return TradeAction.Ignore;
        }

        private bool CrossesFromAbove(List<decimal> ma1, List<decimal> ma2)
        {
            if (ma1.Count != ma2.Count || ma1.Count < 3)
            {
                return false;
            }

            var currentIndex = ma1.Count - 1;
            var prevIndex = ma1.Count - 2;
            var lastBut2Index = ma1.Count - 3;

            return ma1[currentIndex] - ma2[currentIndex] < 0 &&
                   ma1[prevIndex] - ma2[prevIndex] >= 0 &&
                   ma1[lastBut2Index] - ma2[lastBut2Index] > 0;
        }

        private bool CrossesFromBelow(List<decimal> ma1, List<decimal> ma2)
        {
            if (ma1.Count != ma2.Count || ma1.Count < 3)
            {
                return false;
            }

            var currentIndex = ma1.Count - 1;
            var prevIndex = ma1.Count - 2;
            var lastBut2Index = ma1.Count - 3;

            return ma1[currentIndex] - ma2[currentIndex] > 0 &&
                   ma1[prevIndex] - ma2[prevIndex] <= 0 &&
                   ma1[lastBut2Index] - ma2[lastBut2Index] < 0;
        }
    }
}