using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Entities;
using BinanceTrader.Indicators;

namespace BinanceTrader.TradeStrategies
{
    public class OneMinScalpingStrategy : ITradeStrategy
    {
        private readonly int _shortEMAPeriod;
        private readonly int _longEMAPeriod;
        private readonly int _smaPeriod;

        public OneMinScalpingStrategy(int shortEMAPeriod, int longEMAPeriod, int smaPeriod)
        {
            _shortEMAPeriod = shortEMAPeriod;
            _longEMAPeriod = longEMAPeriod;
            _smaPeriod = smaPeriod;
        }

        public TradeActionType GetTradeAction(List<Candle> candles)
        {
            var prices = candles.Select(c => c.ClosePrice).ToList();
            var shortEMA = EMA.Calculate(prices, _shortEMAPeriod);
            var longEMA = EMA.Calculate(prices, _longEMAPeriod);
            var sma = SMA.Calculate(prices, _smaPeriod);

            if (CrossesFromBelow(shortEMA, longEMA) &&
                CrossesFromBelow(shortEMA, sma)
            )
            {
                return TradeActionType.Buy;
            }

            if (CrossesFromAbove(shortEMA, longEMA) &&
                CrossesFromAbove(shortEMA, sma))
            {
                return TradeActionType.Sell;
            }

            return TradeActionType.Ignore;
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