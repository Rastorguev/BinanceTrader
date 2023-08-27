using BinanceApi.Models.Enums;
using BinanceApi.Models.Market;
using BinanceApi.Models.Market.TradingRules;
using BinanceApi.Models.WebSocket;
using BinanceTrader.Tools;

namespace BinanceTrader.Core;

public class OrderDistributor
{
    private readonly string _quoteAsset;

    private readonly decimal _profitRatio;

    private readonly TradingRulesProvider _rulesProvider;

    private readonly ILogger _logger;

    public OrderDistributor(
        string quoteAsset,
        decimal profitRatio,
        TradingRulesProvider rulesProvider,
        ILogger logger)
    {
        _quoteAsset = quoteAsset;
        _profitRatio = profitRatio;
        _rulesProvider = rulesProvider;
        _logger = logger;
    }

    public IReadOnlyList<OrderRequest> SplitIntoBuyOrders(
        decimal freeQuoteAmount,
        IReadOnlyList<string> assets,
        IReadOnlyList<IOrder> openOrders,
        IReadOnlyList<SymbolPrice> prices)
    {
        var requests = new List<OrderRequest>();
        var remainingQuoteAmount = freeQuoteAmount;

        var openOrdersCount = assets.Select(asset =>
        {
            var symbol = SymbolUtils.GetCurrencySymbol(asset, _quoteAsset);
            var count = openOrders.Count(o => o.Symbol == symbol);
            return (symbol, count);
        });

        var minOrdersCountSymbols = openOrdersCount
            .Where(x => _rulesProvider.GetRulesFor(x.symbol).Status == SymbolStatus.Trading)
            .GroupBy(x => x.count)
            .OrderBy(g => g.Key)
            .First()
            .Select(g => g.symbol)
            .ToList();

        var amountPerSymbol = freeQuoteAmount / minOrdersCountSymbols.Count;

        foreach (var symbol in minOrdersCountSymbols)
        {
            try
            {
                var tradingRules = _rulesProvider.GetRulesFor(symbol);

                var symbolPrice = prices.FirstOrDefault(p => p.Symbol == symbol);
                if (symbolPrice == null)
                {
                    continue;
                }

                var currentPrice = symbolPrice.Price;
                var buyPrice =
                    RulesHelper.GetMaxFittingPrice(currentPrice - currentPrice.Percentage(_profitRatio),
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