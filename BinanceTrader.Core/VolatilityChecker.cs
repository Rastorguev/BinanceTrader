using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.API.Csharp.Client.Models.Enums;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public class VolatilityChecker
    {
        [NotNull] private readonly ICandlesProvider _candlesProvider;
        [NotNull] private readonly ILogger _logger;

        public VolatilityChecker([NotNull] ICandlesProvider candlesProvider, [NotNull] ILogger logger)
        {
            _candlesProvider = candlesProvider;
            _logger = logger;
        }

        [NotNull]
        public async Task<Dictionary<string, decimal>> GetAssetsVolatility(
            [NotNull] [ItemNotNull] IEnumerable<string> assets,
            [NotNull] string quoteAsset,
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
                            .NotNull())
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

            await Task.WhenAll(tasks).NotNull();

            return allCandles.ToDictionary(c => c.Key, c => c.Value);
        }
    }
}