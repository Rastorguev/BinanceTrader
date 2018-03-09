using System.Collections.Generic;
using System.Linq;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Indicators;
using BinanceTrader.Tools;

namespace BinanceTrader.Strategies
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

        public TradeAction GetTradeAction(List<Candlestick> candles)
        {
            var prices = candles.Select(c => c.NotNull().Close).ToList();
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