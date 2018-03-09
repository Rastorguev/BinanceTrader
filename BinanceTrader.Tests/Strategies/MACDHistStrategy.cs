using System.Collections.Generic;
using System.Linq;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Indicators;
using BinanceTrader.Tools;

namespace BinanceTrader.Strategies
{
    public class MACDHistStrategy : ITradeStrategy
    {
        private readonly int _shortEMAPeriod;
        private readonly int _longEMAPeriod;
        private readonly int _signalPeriod;

        public MACDHistStrategy(int shortEMAPeriod, int longEMAPeriod, int signalPeriod)
        {
            _shortEMAPeriod = shortEMAPeriod;
            _longEMAPeriod = longEMAPeriod;
            _signalPeriod = signalPeriod;
        }

        public TradeAction GetTradeAction(List<Candlestick> candles)
        {
            var prices = candles.Select(c => c.NotNull().Close).ToList();

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