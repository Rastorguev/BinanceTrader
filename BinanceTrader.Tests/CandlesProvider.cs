using System.Collections.Concurrent;
using BinanceApi.Client;
using BinanceApi.Models.Enums;
using BinanceApi.Models.Market;
using BinanceTrader.Core;
using BinanceTrader.Tools;
using Newtonsoft.Json;

namespace BinanceTrader.Tests;

public class CandlesProvider : ICandlesProvider
{
    private const string DateFormat = "yyyy-MM-dd_hh-mm";
    private const string DirName = "Candles";

    private readonly string _dirPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), DirName);

    private readonly ConcurrentDictionary<string, IReadOnlyList<Candlestick>> _inMemoryCache = new();

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

    private readonly BinanceClient _client;

    public CandlesProvider(BinanceClient client)
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
        var fileName = GenerateFileName(baseAsset, quoteAsset, start, end, interval);

        if (TryGetFromInMemoryCache(fileName, out var cachedInMemory))
        {
            return cachedInMemory;
        }

        var semaphore = _semaphores.GetOrAdd(fileName, new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
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

            var candles = await new CandlesLoader(_client).LoadCandles(baseAsset, quoteAsset, start, end, interval);

            PutToInMemoryCache(candles, fileName);
            PutToDiskCache(candles, fileName);

            return candles;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private void PutToInMemoryCache(IReadOnlyList<Candlestick> candles,
        string key)
    {
        _inMemoryCache.TryAdd(key, candles);
    }

    private bool TryGetFromInMemoryCache(string key, out IReadOnlyList<Candlestick> candles)
    {
        return _inMemoryCache.TryGetValue(key, out candles);
    }

    private void PutToDiskCache(
        IReadOnlyList<Candlestick> candles,
        string fileName)
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

    private bool TryGetFromDiskCache(string fileName, out IReadOnlyList<Candlestick> candles)
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