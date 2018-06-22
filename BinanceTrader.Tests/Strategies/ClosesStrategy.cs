using System.Collections.Generic;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Tools;

namespace BinanceTrader.Strategies
{
    public class ClosesStrategy : ITradingStrategy
    {
        public TradeAction GetTradeAction(IReadOnlyList<Candlestick> candles, int index)
        {
            if (index < 1)
            {
                return TradeAction.Ignore;
            }

            var current = candles[index].NotNull();
            var prev = candles[index - 1].NotNull();

            if (current.Close > prev.Close)
            {
                return TradeAction.Buy;
            }

            if (current.Close < prev.Close)
            {
                return TradeAction.Sell;
            }

            return TradeAction.Ignore;
        }
    }
}