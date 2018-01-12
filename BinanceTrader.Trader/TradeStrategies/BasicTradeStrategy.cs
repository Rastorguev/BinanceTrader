using System.Collections.Generic;
using BinanceTrader.Indicators;

namespace BinanceTrader.TradeStrategies
{
    public class BasicTradeStrategy : ITradeStrategy
    {
        private readonly int _shortEMAPeriod;
        private readonly int _longEMAPeriod;
        private readonly int _signalPeriod;

        public BasicTradeStrategy(int shortEMAPeriod, int longEMAPeriod, int signalPeriod)
        {
            _shortEMAPeriod = shortEMAPeriod;
            _longEMAPeriod = longEMAPeriod;
            _signalPeriod = signalPeriod;
        }

        public TradeAction GetTradeAction(List<decimal> prices)
        {
            if (prices.Count < 2)
            {
                return TradeAction.Ignore;
            }

            var shortEMA = EMA.Calculate(prices, _shortEMAPeriod);
            var longEMA = EMA.Calculate(prices, _longEMAPeriod);
            var macd = MACD.Calculate(shortEMA, longEMA);
            var signal = EMA.Calculate(macd, _signalPeriod);

            var currentIndex = prices.Count - 1;
            var prevIndex = prices.Count - 2;

            var currentHist = MACD.GetHistValue(macd[currentIndex], signal[currentIndex]);
            var prevHist = MACD.GetHistValue(macd[prevIndex], signal[prevIndex]);

            if (currentHist >= 0 && prevHist < 0)
            {
                return TradeAction.Buy;
            }
            if (currentHist < 0 && prevHist >= 0)
            {
                return TradeAction.Sell;
            }

            return TradeAction.Ignore;
        }
    }
}