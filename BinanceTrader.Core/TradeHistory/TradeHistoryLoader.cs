using BinanceApi.Domain.Interfaces;
using BinanceApi.Models.Account;
using BinanceApi.Models.Enums;
using BinanceApi.Models.Extensions;
using BinanceApi.Models.Market;

namespace BinanceTrader.Core.TradeHistory;

public class TradeHistoryLoader : ITradeHistoryProvider
{
    private readonly IBinanceClient _client;

    public TradeHistoryLoader(IBinanceClient client)
    {
        _client = client;
    }

    public async Task<Dictionary<string, IReadOnlyList<Trade>>> LoadTradeHistory(
        IReadOnlyList<string> baseAssets,
        string quoteAsset,
        DateTime start,
        DateTime end)
    {
        var assetsTradeHistory = new Dictionary<string, IReadOnlyList<Trade>>();

        foreach (var baseAsset in baseAssets)
        {
            var symbol = SymbolUtils.GetCurrencySymbol(baseAsset, quoteAsset);

            Console.WriteLine($"Trade History Load Started: {symbol}");

            var assetTradeHistory = await LoadTradeHistory(baseAsset, quoteAsset, start, end);

            Console.WriteLine($"Trade History Load Finished: {symbol}");

            assetsTradeHistory.Add(baseAsset, assetTradeHistory);
        }

        return assetsTradeHistory;
    }

    private async Task<IReadOnlyList<Trade>> LoadTradeHistory(
        string baseAsset,
        string quoteAsset,
        DateTime start,
        DateTime end)
    {
        if (end <= start)
        {
            throw new Exception("End date  should be grater then start date.");
        }

        var symbol = $"{baseAsset}{quoteAsset}";
        var tradeHistory = new List<Trade>();

        var startDate = start;

        while (startDate < end)
        {
            var endDate = startDate.AddDays(1);
            if (endDate > end)
            {
                endDate = end;
            }

            var trades = (await _client.GetTradeList(symbol, startDate, endDate)).ToList();

            //To avoid too much requests error
            await Task.Delay(300);

            tradeHistory.AddRange(trades);

            startDate = endDate;
        }

        //Select unique trades
        tradeHistory = tradeHistory
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .OrderByDescending(x => x.LocalTime).ToList();

        return tradeHistory;
    }
}