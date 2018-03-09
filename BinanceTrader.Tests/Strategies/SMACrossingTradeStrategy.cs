using System.Collections.Generic;
using System.Linq;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Indicators;
using BinanceTrader.Tools;

namespace BinanceTrader.Strategies
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

        public TradeAction GetTradeAction(List<Candlestick> candles)
        {
            var prices = candles.Select(c => c.NotNull().Close).ToList();

            if (prices.Count < _longSMAPeriod)
            {
                return TradeAction.Ignore;
            }

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