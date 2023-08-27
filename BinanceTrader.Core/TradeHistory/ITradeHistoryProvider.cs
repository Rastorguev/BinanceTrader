using BinanceApi.Models.Account;

namespace BinanceTrader.Core.TradeHistory;

public interface ITradeHistoryProvider
{
    Task<Dictionary<string, IReadOnlyList<Trade>>> LoadTradeHistory(
        IReadOnlyList<string> baseAssets,
        string quoteAsset,
        DateTime start,
        DateTime end);
}