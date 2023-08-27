using BinanceApi.Models.Enums;
using BinanceApi.Models.Market;

namespace BinanceTrader.Core.Candles;

public interface ICandlesProvider
{
    Task<IReadOnlyList<Candlestick>> LoadCandles(
        string baseAsset,
        string quoteAsset,
        DateTime start,
        DateTime end,
        TimeInterval interval);
}