using BinanceApi.Domain.Interfaces;
using BinanceApi.Models.Enums;
using BinanceApi.Models.Extensions;
using BinanceApi.Models.Market;

namespace BinanceTrader.Core.Candles;

public class CandlesLoader : ICandlesProvider
{
    private readonly IBinanceClient _client;

    public CandlesLoader(IBinanceClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<Candlestick>> LoadCandles(
        string baseAsset,
        string quoteAsset,
        DateTime start,
        DateTime end,
        TimeInterval interval)
    {
        const int maxRange = 1000;

        var tasks = new List<Task<IEnumerable<Candlestick>>>();

        while (start < end)
        {
            var intervalMinutes = maxRange * interval.ToMinutes();
            var rangeEnd = (end - start).TotalMinutes > intervalMinutes
                ? start.AddMinutes(intervalMinutes)
                : end;

            var symbol = $"{baseAsset}{quoteAsset}";

            tasks.Add(_client.GetCandleSticks(symbol, interval, start, rangeEnd, maxRange));
            start = rangeEnd;
        }

        var candles = (await Task.WhenAll(tasks)).SelectMany(c => c).ToList();

        var orderedCandles = candles.OrderBy(c => c.OpenLocalTime).ToList();
        return orderedCandles;
    }
}