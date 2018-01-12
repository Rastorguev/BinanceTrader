using System.Collections.Generic;
using BinanceTrader.Indicators;

namespace BinanceTrader.TradeStrategies
{
    public class EMATradeStrategy : ITradeStrategy
    {
        private readonly int _shortEMAPeriod;
        private readonly int _longEMAPeriod;

        public EMATradeStrategy(int shortEMAPeriod, int longEMAPeriod)
        {
            _shortEMAPeriod = shortEMAPeriod;
            _longEMAPeriod = longEMAPeriod;
        }

        public TradeAction GetTradeAction(List<decimal> prices)
        {
            const int minCount = 3;

            if (prices.Count < minCount)
            {
                return TradeAction.Ignore;
            }

            var currentIndex = prices.Count - 1;
            var prevIndex = prices.Count - 2;
            var prevPrevIndex = prices.Count - 3;

            var shortEMA = EMA.Calculate(prices, _shortEMAPeriod);
            var longEMA = EMA.Calculate(prices, _longEMAPeriod);

            if (shortEMA[currentIndex] - longEMA[currentIndex] > 0 &&
                shortEMA[prevIndex] - longEMA[prevIndex] <= 0 &&
                shortEMA[prevPrevIndex] - longEMA[prevPrevIndex] < 0)
            {
                return TradeAction.Buy;
            }

            if (shortEMA[currentIndex] - longEMA[currentIndex] < 0 &&
                shortEMA[prevIndex] - longEMA[prevIndex] >= 0 &&
                shortEMA[prevPrevIndex] - longEMA[prevPrevIndex] > 0)
            {
                return TradeAction.Sell;
            }

            return TradeAction.Ignore;
        }
    }
}