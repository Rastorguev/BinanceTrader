using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using Binance.API.Csharp.Client.Models.Market.TradingRules;
using Binance.API.Csharp.Client.Models.WebSocket;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Trader;

public class OrderDistributor
{
    [NotNull]
    private readonly string _quoteAsset;

    private readonly decimal _profitRatio;

    [NotNull]
    private readonly TradingRulesProvider _rulesProvider;

    [NotNull]
    private readonly ILogger _logger;

    public OrderDistributor(
        [NotNull] string quoteAsset,
        decimal profitRatio,
        [NotNull] TradingRulesProvider rulesProvider,
        [NotNull] ILogger logger)
    {
        _quoteAsset = quoteAsset;
        _profitRatio = profitRatio;
        _rulesProvider = rulesProvider;
        _logger = logger;
    }

    [NotNull]
    public IReadOnlyList<OrderRequest> SplitIntoBuyOrders(
        decimal freeQuoteAmount,
        [NotNull] [ItemNotNull] IReadOnlyList<string> assets,
        [NotNull] IReadOnlyList<IOrder> openOrders,
        [NotNull] IReadOnlyList<SymbolPrice> prices)
    {
        var requests = new List<OrderRequest>();
        var remainingQuoteAmount = freeQuoteAmount;

        var openOrdersCount = assets.Select(asset =>
        {
            var symbol = SymbolUtils.GetCurrencySymbol(asset, _quoteAsset);
            var count = openOrders.Count(o => o.NotNull().Symbol == symbol);
            return (symbol, count);
        });

        var minOrdersCountSymbols = openOrdersCount
            .Where(x => _rulesProvider.GetRulesFor(x.symbol).Status == SymbolStatus.Trading)
            .GroupBy(x => x.count)
            .OrderBy(g => g.Key)
            .First().NotNull()
            .Select(g => g.symbol)
            .ToList();

        var amountPerSymbol = freeQuoteAmount / minOrdersCountSymbols.Count;

        foreach (var symbol in minOrdersCountSymbols)
        {
            try
            {
                var tradingRules = _rulesProvider.GetRulesFor(symbol);

                var symbolPrice = prices.FirstOrDefault(p => p.NotNull().Symbol == symbol);
                if (symbolPrice == null)
                {
                    continue;
                }

                var currentPrice = symbolPrice.Price;
                var buyPrice =
                    RulesHelper.GetMaxFittingPrice(currentPrice - currentPrice.Percents(_profitRatio),
                        tradingRules);
                var minNotionalQty = RulesHelper.GetMinNotionalQty(buyPrice, tradingRules);
                var fittingAmount =
                    RulesHelper.GetFittingBaseAmount(amountPerSymbol, buyPrice, tradingRules);
                var maxFittingQty = RulesHelper.GetMaxFittingQty(fittingAmount, tradingRules);

                var qty = Math.Max(minNotionalQty, maxFittingQty);
                var quoteAmount = qty * buyPrice;

                if (remainingQuoteAmount >= quoteAmount)
                {
                    remainingQuoteAmount -= quoteAmount;
                    requests.Add(new OrderRequest(symbol, OrderSide.Buy, qty, buyPrice));
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        return requests;
    }
}