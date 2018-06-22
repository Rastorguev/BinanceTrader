using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models;
using Binance.API.Csharp.Client.Models.Enums;
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
        [NotNull] private readonly string _dirPath = $@"D:\{DirName}";

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
            if (TryGetFromCache(baseAsset, quoteAsset, start, end, interval, out var cached))
            {
                return cached;
            }

            var candles = await LoadCandles(baseAsset, quoteAsset, start, end, interval);

            SaveToCache(candles, baseAsset, quoteAsset, start, end, interval);

            return candles;
        }

        private void SaveToCache(
            [NotNull] IReadOnlyList<Candlestick> candles,
            string baseAsset,
            string quoteAsset,
            DateTime start,
            DateTime end,
            TimeInterval interval)
        {
            var serialized = JsonConvert.SerializeObject(candles);

            var fileName = GenerateFileName(
                baseAsset,
                quoteAsset,
                start,
                end,
                interval);

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

        private bool TryGetFromCache(string baseAsset,
            string quoteAsset,
            DateTime start,
            DateTime end,
            TimeInterval interval,
            out List<Candlestick> candles
        )
        {
            candles = new List<Candlestick>();

            var fileName = GenerateFileName(
                baseAsset,
                quoteAsset,
                start,
                end, interval);

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