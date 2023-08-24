using System.Collections.Concurrent;
using BinanceApi.Models.Enums;
using BinanceTrader.Tools;

namespace BinanceTrader.Core;

public class VolatilityChecker
{
    private readonly ICandlesProvider _candlesProvider;

    private readonly ILogger _logger;

    public VolatilityChecker(ICandlesProvider candlesProvider, ILogger logger)
    {
        _candlesProvider = candlesProvider;
        _logger = logger;
    }

    public async Task<Dictionary<string, decimal>> GetAssetsVolatility(
        IEnumerable<string> assets,
        string quoteAsset,
        DateTime startTime,
        DateTime endTime,
        TimeInterval interval)
    {
        var allCandles = new ConcurrentDictionary<string, decimal>();
        var tasks = assets.Select(async asset =>
            {
                try
                {
                    var candles = (await _candlesProvider
                            .LoadCandles(asset, quoteAsset, startTime, endTime, interval)
                        )
                        .ToList();

                    if (candles.Any())
                    {
                        var volatility = TechAnalyzer.CalculateVolatilityIndex(candles);

                        allCandles.TryAdd(asset, volatility);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex);
                }
            })
            .ToList();

        await Task.WhenAll(tasks);

        return allCandles.ToDictionary(c => c.Key, c => c.Value);
    }
}