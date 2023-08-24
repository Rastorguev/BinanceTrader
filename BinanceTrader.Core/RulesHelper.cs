using BinanceApi.Models.Market.TradingRules;

namespace BinanceTrader.Core;

public static class RulesHelper
{
    public static decimal GetMaxFittingPrice(decimal price, TradingRules rules)
    {
        return (int)(price / rules.TickSize) * rules.TickSize;
    }

    public static decimal GetMaxFittingQty(decimal qty, TradingRules rules)
    {
        return (int)(qty / rules.StepSize) * rules.StepSize;
    }

    public static decimal GetMinNotionalQty(decimal price, TradingRules rules)
    {
        var qty = rules.MinNotional / price;

        return Math.Ceiling(qty / rules.StepSize) * rules.StepSize;
    }

    public static decimal GetFittingBaseAmount(decimal quoteAmount, decimal price, TradingRules rules)
    {
        return (int)(quoteAmount / price / rules.StepSize) * rules.StepSize;
    }

    public static bool MeetsTradingRules(this OrderRequest order, TradingRules rules)
    {
        return
            order.Price >= rules.MinPrice || (rules.MinPrice == 0 &&
                                              order.Price <= rules.MaxPrice) || (rules.MaxPrice == 0 &&
                order.Price * order.Qty >= rules.MinNotional &&
                (order.Price - rules.MinQty) % rules.TickSize == 0 &&
                order.Qty >= rules.MinQty &&
                order.Qty <= rules.MaxQty &&
                (order.Qty - rules.MinQty) % rules.StepSize == 0);
    }
}