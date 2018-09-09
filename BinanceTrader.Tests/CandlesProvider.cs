using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Extensions;
using Binance.API.Csharp.Client.Models.Market;
using BinanceTrader.Tools;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace BinanceTrader
{
    public interface ICandlesProvider
    {
        [NotNull]
        Task<IReadOnlyList<Candlestick>> GetCandles(
            string baseAsset,
            string quoteAsset,
            DateTime start,
            DateTime end,
            TimeInterval interval);
    }

    public class CandlesProvider : ICandlesProvider
    {
        private const string DateFormat = "yyyy-MM-dd_hh-mm";
        private const string DirName = "Candles";
        [NotNull] private readonly string _dirPath = $@"C:\{DirName}";

        [NotNull] private readonly ConcurrentDictionary<string, IReadOnlyList<Candlestick>> _inMimoryCache =
            new ConcurrentDictionary<string, IReadOnlyList<Candlestick>>();

        [NotNull] private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        [NotNull] private readonly BinanceClient _client;

        public CandlesProvider([NotNull] BinanceClient client)
        {
            _client = client;
        }

        public async Task<IReadOnlyList<Candlestick>> GetCandles(
            string baseAsset,
            string quoteAsset,
            DateTime start,
            DateTime end,
            TimeInterval interval)
        {
            var fileName = GenerateFileName(baseAsset, quoteAsset, start, end, interval);

            if (TryGetFromInMemoryCache(fileName, out var cachedInMemory))
            {
                return cachedInMemory;
            }

            var semaphore = _semaphores.GetOrAdd(fileName, new SemaphoreSlim(1, 1)).NotNull();
            await semaphore.WaitAsync().NotNull();
            try
            {
                if (TryGetFromInMemoryCache(fileName, out cachedInMemory))
                {
                    return cachedInMemory;
                }

                if (TryGetFromDiskCache(fileName, out var cachedOnDisk))
                {
                    PutToInMemoryCache(cachedOnDisk, fileName);

                    return cachedOnDisk;
                }

                var candles = await LoadCandles(baseAsset, quoteAsset, start, end, interval);

                PutToInMemoryCache(candles, fileName);
                PutToDiskCache(candles, fileName);

                return candles;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void PutToInMemoryCache([NotNull] IReadOnlyList<Candlestick> candles,
            [NotNull] string key)
        {
            _inMimoryCache.TryAdd(key, candles);
        }

        private bool TryGetFromInMemoryCache([NotNull] string key, out IReadOnlyList<Candlestick> candles)
        {
            return _inMimoryCache.TryGetValue(key, out candles);
        }

        private void PutToDiskCache(
            [NotNull] IReadOnlyList<Candlestick> candles,
            [NotNull] string fileName)
        {
            var serialized = JsonConvert.SerializeObject(candles);

            if (!Directory.Exists(_dirPath))
            {
                Directory.CreateDirectory(_dirPath);
            }

            var path = Path.Combine(_dirPath, fileName);

            if (!File.Exists(path))
            {
                using (var sw = File.CreateText(path))
                {
                    sw.Write(serialized);
                }
            }
        }

        private bool TryGetFromDiskCache(
            [NotNull] string fileName,
            out IReadOnlyList<Candlestick> candles
        )
        {
            candles = new List<Candlestick>();
            var path = Path.Combine(_dirPath, fileName);

            if (!File.Exists(path))
            {
                return false;
            }

            string serialized;
            using (var sr = File.OpenText(path))
            {
                serialized = sr.ReadToEnd();
            }

            candles = JsonConvert.DeserializeObject<List<Candlestick>>(serialized);

            return true;
        }

        [NotNull]
        [ItemNotNull]
        private async Task<IReadOnlyList<Candlestick>> LoadCandles(
            string baseAsset,
            string quoteAsset,
            DateTime start,
            DateTime end,
            TimeInterval interval)
        {
            const int maxRange = 500;

            var tasks = new List<Task<IEnumerable<Candlestick>>>();

            while (start < end)
            {
                var intervalMinutes = maxRange * interval.ToMinutes();
                var rangeEnd = (end - start).TotalMinutes > intervalMinutes
                    ? start.AddMinutes(intervalMinutes)
                    : end;

                var symbol = $"{baseAsset}{quoteAsset}";

                tasks.Add(_client.GetCandleSticks(symbol, interval, start, rangeEnd));
                start = rangeEnd;
            }

            var candles = (await Task.WhenAll(tasks).NotNull()).SelectMany(c => c).ToList();

            var orderedCandles = candles.OrderBy(c => c.NotNull().OpenTime).ToList();
            return orderedCandles;
        }

        [NotNull]
        private string GenerateFileName(
            string baseAsset,
            string quoteAsset,
            DateTime startTime,
            DateTime endTime,
            TimeInterval interval)
        {
            var name = string.Join("__",
                baseAsset,
                quoteAsset,
                startTime.ToString(DateFormat),
                endTime.ToString(DateFormat),
                interval.ToString());

            return $"{name}.json";
        }
    }
}