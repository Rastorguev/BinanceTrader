using System.Collections.Generic;
using System.Linq;
using BinanceTrader.Analytics.Indicators;
using BinanceTrader.Core.Entities;
using BinanceTrader.Core.Entities.Enums;

namespace BinanceTrader.Analytics.TradeStrategies
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

        public TradeActionType GetTradeAction(List<Candle> candles)
        {
            var prices = candles.Select(c => c.ClosePrice).ToList();
            var shortEMA = EMA.Calculate(prices, _shortEMAPeriod);
            var longEMA = EMA.Calculate(prices, _longEMAPeriod);

            if (IndicatorsUtils.CrossesFromBelow(shortEMA, longEMA))
            {
                return TradeActionType.Buy;
            }

            if (IndicatorsUtils.CrossesFromAbove(shortEMA, longEMA))
            {
                return TradeActionType.Sell;
            }

            return TradeActionType.Ignore;
        }
    }
}