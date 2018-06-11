using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Binance.API.Csharp.Client.Domain.Interfaces;
using Binance.API.Csharp.Client.Models;
using Binance.API.Csharp.Client.Models.Account;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using Binance.API.Csharp.Client.Models.Market.TradingRules;
using Binance.API.Csharp.Client.Models.WebSocket;
using BinanceTrader.Tools;
using JetBrains.Annotations;
using Balance = Binance.API.Csharp.Client.Models.Account.Balance;

namespace BinanceTrader.Trader
{
    public class RabbitTrader
    {
        private readonly TimeSpan _scheduleInterval = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _sellWaitingTime = TimeSpan.FromHours(4);
        private readonly TimeSpan _buyWaitingTime = TimeSpan.FromHours(1);
        private const decimal MinProfitRatio = 2;
        private const decimal MaxProfitRatio = 3;
        private const string QuoteAsset = "ETH";
        private const string FeeAsset = "BNB";
        private const string UsdtAsset = "USDT";
        private const decimal MinOrderSize = 0.015m;

        [NotNull] private readonly IBinanceClient _client;
        [NotNull] private readonly ILogger _logger;
        [NotNull] private readonly TradingRulesProvider _rulesProvider;

        [NotNull] [ItemNotNull] private IReadOnlyList<string> _assets = new List<string>();

        public RabbitTrader(
            [NotNull] IBinanceClient client,
            [NotNull] ILogger logger)
        {
            _logger = logger;
            _client = client;
            _rulesProvider = new TradingRulesProvider(client);
        }

        public async void Start()
        {
            while (true)
            {
                await Task.WhenAll(new List<Task> {ExecuteScheduledTasks(), Task.Delay(_scheduleInterval)}).NotNull();
            }

            // ReSharper disable FunctionNeverReturns
        }
        // ReSharper restore FunctionNeverReturns

        public async Task ExecuteScheduledTasks()
        {
            try
            {
                await _rulesProvider.UpdateRulesIfNeeded();
                _assets = _rulesProvider.GetBaseAssetsFor(QuoteAsset).Where(r => r != FeeAsset).ToList();

                await BuyFeeCurrencyIfNeeded();
                await CheckOrders();
                await LogFundsState();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async Task CheckOrders()
        {
            await CancelExpiredOrders();
            await PlaceSellOrders();
            await PlaceBuyOrders();
        }

        private async Task CancelExpiredOrders()
        {
            try
            {
                var openOrders = (await _client.GetCurrentOpenOrders().NotNull()).NotNull().ToList();
                var now = DateTime.Now;
                var expiredSellOrders = openOrders
                    .Where(o => o.NotNull().Side == OrderSide.Sell &&
                                now.ToLocalTime() - o.NotNull().UnixTime.GetTime().ToLocalTime() > _sellWaitingTime)
                    .ToList();
                var expiredBuyOrders = openOrders
                    .Where(o => o.NotNull().Side == OrderSide.Buy &&
                                now.ToLocalTime() - o.NotNull().UnixTime.GetTime().ToLocalTime() > _buyWaitingTime)
                    .ToList();

                foreach (var order in expiredSellOrders.Concat(expiredBuyOrders))
                {
                    await CancelOrder(order.NotNull());
                    _logger.LogOrder("Canceled", order);
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async Task PlaceSellOrders()
        {
            try
            {
                var freeBalances
                    = (await _client.GetAccountInfo().NotNull()).NotNull().Balances.NotNull()
                    .Where(b => b.NotNull().Free > 0 && _assets.Contains(b.Asset)).ToList();

                foreach (var balance in freeBalances)
                {
                    try
                    {
                        var symbol = SymbolUtils.GetCurrencySymbol(balance.NotNull().Asset.NotNull(), QuoteAsset);
                        var price = await GetActualPrice(symbol, OrderSide.Sell);
                        var tradingRules = _rulesProvider.GetRulesFor(symbol);

                        var sellAmounts =
                            OrderDistributor.SplitIntoSellOrders(
                                balance.Free,
                                MinOrderSize,
                                price,
                                tradingRules.StepSize);

                        var profitStepSize = GetProfitStepSize(sellAmounts.Count);
                        var profitRatio = MinProfitRatio;

                        foreach (var amount in sellAmounts)
                        {
                            var sellPrice =
                                AdjustPriceAccordingRules(price + price.Percents(profitRatio), tradingRules);
                            var orderRequest = new OrderRequest(symbol, OrderSide.Sell, amount, sellPrice);

                            if (MeetsTradingRules(orderRequest))
                            {
                                await PlaceOrder(orderRequest);
                                profitRatio += profitStepSize;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async Task PlaceBuyOrders()
        {
            try
            {
                var freeQuoteBalance = (await _client.GetAccountInfo().NotNull()).NotNull().Balances.NotNull()
                    .First(b => b.NotNull().Asset == QuoteAsset).NotNull().Free;

                var gains = await GetPricesGain().NotNull();
                var amounts =
                    OrderDistributor.SplitIntoBuyOrders(freeQuoteBalance, MinOrderSize, gains);

                foreach (var symbolAmounts in amounts)
                {
                    try
                    {
                        var symbol = symbolAmounts.Key;
                        var price = await GetActualPrice(symbol, OrderSide.Buy);
                        var tradingRules = _rulesProvider.GetRulesFor(symbol);
                        var buyAmounts = symbolAmounts.Value.NotNull();

                        foreach (var quoteAmount in buyAmounts)
                        {
                            var buyPrice = AdjustPriceAccordingRules(price, tradingRules);
                            var baseAmount =
                                OrderDistributor.GetFittingBaseAmount(quoteAmount, buyPrice, tradingRules.StepSize);
                            var orderRequest = new OrderRequest(symbol, OrderSide.Buy, baseAmount, buyPrice);

                            if (MeetsTradingRules(orderRequest))
                            {
                                await PlaceOrder(orderRequest);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async Task<decimal> GetActualPrice(string symbol, OrderSide orderSide)
        {
            var priceInfo = await GetPrices(symbol).NotNull();

            switch (orderSide)
            {
                case OrderSide.Buy:
                    return priceInfo.AskPrice;
                case OrderSide.Sell:
                    return priceInfo.BidPrice;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderSide), orderSide, null);
            }
        }

        private async Task<NewOrder> PlaceOrder([NotNull] OrderRequest orderRequest,
            OrderType orderType = OrderType.Limit,
            TimeInForce timeInForce = TimeInForce.GTC)
        {
            var newOrder = (await _client.PostNewOrder(
                        orderRequest.Symbol,
                        orderRequest.Qty,
                        orderRequest.Price,
                        orderRequest.Side,
                        orderType,
                        timeInForce)
                    .NotNull())
                .NotNull();

            _logger.LogOrder("Placed", newOrder);

            return newOrder;
        }

        private bool MeetsTradingRules([NotNull] OrderRequest orderRequest)
        {
            var rules = _rulesProvider.GetRulesFor(orderRequest.Symbol);
            if (orderRequest.MeetsTradingRules(rules))
            {
                return true;
            }

            _logger.LogOrderRequest("OrderRequestDoesNotMeetRules", orderRequest);

            return false;
        }

        private async Task<PriceChangeInfo> GetPrices(string symbol)
        {
            var priceInfo = (await _client.GetPriceChange24H(symbol).NotNull()).NotNull().First();

            return priceInfo;
        }

        private async Task<CanceledOrder> CancelOrder([NotNull] IOrder order)
        {
            var canceledOrder = await _client.CancelOrder(order.Symbol, order.OrderId).NotNull();

            _logger.LogOrder("Canceled", order);

            return canceledOrder;
        }

        private async Task BuyFeeCurrencyIfNeeded()
        {
            try
            {
                const int qty = 1;
                var feeSymbol = SymbolUtils.GetCurrencySymbol(FeeAsset, QuoteAsset);

                var balance = (await _client.GetAccountInfo().NotNull()).NotNull().Balances.NotNull().ToList();

                var feeAmount = balance.First(b => b.NotNull().Asset == FeeAsset).NotNull().Free;
                var quoteAmount = balance.First(b => b.NotNull().Asset == QuoteAsset).NotNull().Free;
                var price = await GetActualPrice(feeSymbol, OrderSide.Buy);

                if (feeAmount < 1 && quoteAmount >= price * qty)
                {
                    var orderRequest = new OrderRequest(feeSymbol, OrderSide.Buy, qty, price);

                    if (MeetsTradingRules(orderRequest))
                    {
                        var order = await PlaceOrder(orderRequest, OrderType.Market, TimeInForce.IOC).NotNull();
                        var status = order.Status;
                        var executedQty = order.ExecutedQty;

                        _logger.LogMessage("BuyFeeCurrency",
                            $"Status {status}, Quantity {executedQty}, Price {price}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async Task LogFundsState()
        {
            try
            {
                var prices = (await _client.GetAllPrices().NotNull().NotNull()).ToList();
                var assetsAveragePrice = GetTradingAssetsAveragePrice(prices);

                var funds = (await _client.GetAccountInfo().NotNull()).NotNull()
                    .Balances.NotNull().Where(b => b.NotNull().Free + b.NotNull().Locked > 0).ToList();

                var quoteUsdtSymbol = SymbolUtils.GetCurrencySymbol(QuoteAsset, UsdtAsset);
                var quoteTotal = GetFundsTotal(funds, prices);
                var usdtTotal = quoteTotal * prices.First(p => p.NotNull().Symbol == quoteUsdtSymbol).NotNull().Price;

                _logger.LogMessage("Funds", new Dictionary<string, string>
                {
                    {"Quote", quoteTotal.Round().ToString(CultureInfo.InvariantCulture)},
                    {"Usdt", usdtTotal.Round().ToString(CultureInfo.InvariantCulture)},
                    {"AverageAssetPrice", assetsAveragePrice.Round().ToString(CultureInfo.InvariantCulture)}
                });
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private static decimal GetFundsTotal(
            [NotNull] IReadOnlyList<Balance> funds,
            [NotNull] IReadOnlyList<SymbolPrice> prices)
        {
            var total = 0m;
            foreach (var fund in funds)
            {
                var assetTotal = fund.NotNull().Free + fund.NotNull().Locked;
                if (fund.NotNull().Asset == QuoteAsset)
                {
                    total += assetTotal;
                }
                else
                {
                    var symbol = $"{fund.NotNull().Asset}{QuoteAsset}";
                    total += assetTotal * prices.First(p => p.NotNull().Symbol == symbol).NotNull().Price;
                }
            }

            return total;
        }

        private static decimal GetProfitStepSize(int ordersCount)
        {
            var stepSize = ordersCount > 0 ? (MaxProfitRatio - MinProfitRatio) / ordersCount : 0;

            return stepSize.Round();
        }

        private decimal GetTradingAssetsAveragePrice([NotNull] IReadOnlyList<SymbolPrice> prices)
        {
            var symbols = _assets.Select(a => SymbolUtils.GetCurrencySymbol(a, QuoteAsset));
            var tradingAssetsPrices =
                prices.Where(p => symbols.Contains(p.NotNull().Symbol)).Select(p => p.Price).ToList();
            return tradingAssetsPrices.Average().Round();
        }

        [NotNull]
        private async Task<Dictionary<string, decimal>> GetPricesGain()
        {
            var symbols = _assets.Select(a => SymbolUtils.GetCurrencySymbol(a, QuoteAsset));

            var tasks = symbols.Select(async s => (symbol: s,
                candles: (await LoadCandlesForPriceGain(s).NotNull()).OrderBy(c => c.OpenTime)));

            var candles = (await Task.WhenAll(tasks).NotNull()).NotNull();
            var gains = candles
                .Select(c =>
                {
                    var result = (asset: c.symbol, gain: decimal.MinValue);

                    if (!candles.Any())
                    {
                        return result;
                    }

                    var min = c.candles.First().NotNull().Low;
                    var max = c.candles.Last().NotNull().High;

                    result.gain = MathUtils.Gain(min, max).Round();

                    return result;
                })
                .OrderByDescending(c => c.gain)
                .ToDictionary(x => x.asset, x => x.gain);

            return gains;
        }

        private async Task<IEnumerable<Candlestick>> LoadCandlesForPriceGain(string s)
        {
            var candles = new List<Candlestick>();
            try
            {
                candles = (await _client.GetCandleSticks(s, TimeInterval.Minutes_1, null, null, 30).NotNull())
                    .NotNull().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }

            return candles;
        }

        private decimal AdjustPriceAccordingRules(decimal price, [NotNull] ITradingRules rules)
        {
            return (int) (price / rules.TickSize) * rules.TickSize;
        }
    }
}