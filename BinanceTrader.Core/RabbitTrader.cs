using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Binance.API.Csharp.Client.Domain.Interfaces;
using Binance.API.Csharp.Client.Models;
using Binance.API.Csharp.Client.Models.Account;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using Binance.API.Csharp.Client.Models.Market.TradingRules;
using Binance.API.Csharp.Client.Models.WebSocket;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public class RabbitTrader
    {
        private static readonly TimeSpan OrdersCheckInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan FundsCheckInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan StreamResetInterval = TimeSpan.FromMinutes(30);

        private const decimal MinProfitRatio = 1m;
        private const decimal MaxProfitRatio = 1.1m;
        private const string QuoteAsset = "ETH";
        private const string FeeAsset = "BNB";
        private const decimal MinOrderSize = 0.015m;
        private readonly TimeSpan _sellWaitingTime = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _buyWaitingTime = TimeSpan.FromMinutes(1);
        private const int MaxDegreeOfParallelism = 5;

        [NotNull] private readonly Timer _ordersCheckTimer = new Timer
        {
            Interval = OrdersCheckInterval.TotalMilliseconds,
            AutoReset = true
        };

        [NotNull] private readonly Timer _fundsCheckTimer = new Timer
        {
            Interval = FundsCheckInterval.TotalMilliseconds,
            AutoReset = true
        };

        [NotNull] private readonly Timer _streamResetTimer = new Timer
        {
            Interval = StreamResetInterval.TotalMilliseconds,
            AutoReset = true
        };

        [NotNull] private readonly IBinanceClient _client;
        [NotNull] private readonly ILogger _logger;
        [NotNull] private readonly TradingRulesProvider _rulesProvider;

        [NotNull] [ItemNotNull] private IReadOnlyList<string> _assets = new List<string>();
        [NotNull] private readonly FundsStateChecker _fundsStateChecker;
        private string _listenKey;

        public RabbitTrader(
            [NotNull] IBinanceClient client,
            [NotNull] ILogger logger)
        {
            _logger = logger;
            _client = client;
            _rulesProvider = new TradingRulesProvider(client);
            _fundsStateChecker = new FundsStateChecker(_client, _logger, QuoteAsset);

            _fundsCheckTimer.Elapsed += OnFundsEvent;
            _streamResetTimer.Elapsed += OnStreamResetEvent;
            _ordersCheckTimer.Elapsed += OnOrdersCheckEvent;
        }

        public async void Start()
        {
            try
            {
                await _rulesProvider.UpdateRulesIfNeeded();
                _assets = _rulesProvider.GetBaseAssetsFor(QuoteAsset).Where(r => r != FeeAsset).ToList();
                _fundsStateChecker.Assets = _assets;

                await ResetOrderUpdatesListening();
                await BuyFeeCurrencyIfNeeded();
                await CheckOrders();
                await _fundsStateChecker.LogFundsState();

                _ordersCheckTimer.Start();
                _streamResetTimer.Start();
                _fundsCheckTimer.Start();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async void OnOrdersCheckEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                await _rulesProvider.UpdateRulesIfNeeded();
                _assets = _rulesProvider.GetBaseAssetsFor(QuoteAsset).Where(r => r != FeeAsset).ToList();
                _fundsStateChecker.Assets = _assets;

                await BuyFeeCurrencyIfNeeded();
                await CheckOrders();
                await KeepStreamAlive();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async void OnFundsEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                await _fundsStateChecker.LogFundsState();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async void OnStreamResetEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                await ResetOrderUpdatesListening();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async Task ResetOrderUpdatesListening()
        {
            await StopListenOrderUpdates();
            StartListenOrderUpdates();
        }

        private void StartListenOrderUpdates()
        {
            _listenKey = _client.ListenUserDataEndpoint(m => { }, OnTrade, m => { });
        }

        private async Task StopListenOrderUpdates()
        {
            if (_listenKey != null)
            {
                await _client.CloseUserStream(_listenKey).NotNull();
            }
        }

        private async Task KeepStreamAlive()
        {
            if (_listenKey != null)
            {
                await _client.KeepAliveUserStream(_listenKey).NotNull();
            }
        }

        private async void OnTrade([NotNull] OrderOrTradeUpdatedMessage message)
        {
            try
            {
                var baseAsset = SymbolUtils.GetBaseAsset(message.Symbol.NotNull(), QuoteAsset);

                if (message.Status != OrderStatus.Filled ||
                    baseAsset == FeeAsset)
                {
                    return;
                }

                _logger.LogOrder("Completed", message);

                switch (message.Side)
                {
                    case OrderSide.Buy:
                        var sellRequest = CreateSellOrder(message);
                        if (MeetsTradingRules(sellRequest))
                        {
                            await PlaceOrder(sellRequest);
                        }

                        break;

                    case OrderSide.Sell:
                        var buyRequest = CreateBuyOrder(message);
                        if (MeetsTradingRules(buyRequest))
                        {
                            await PlaceOrder(buyRequest);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(OrderSide));
                }
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

                Parallel.ForEach(
                    expiredSellOrders.Concat(expiredBuyOrders),
                    new ParallelOptions {MaxDegreeOfParallelism = MaxDegreeOfParallelism},
                    order =>
                    {
                        try
                        {
                            CancelOrder(order.NotNull()).Wait();
                            _logger.LogOrder("Canceled", order);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogException(ex);
                        }
                    });
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

                Parallel.ForEach(
                    freeBalances,
                    new ParallelOptions {MaxDegreeOfParallelism = MaxDegreeOfParallelism},
                    balance =>
                    {
                        try
                        {
                            var symbol = SymbolUtils.GetCurrencySymbol(balance.NotNull().Asset.NotNull(), QuoteAsset);
                            var price = GetActualPrice(symbol, OrderSide.Sell).Result;
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
                                    PlaceOrder(orderRequest).Wait();
                                    profitRatio += profitStepSize;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogException(ex);
                        }
                    });
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

                var openOrders = (await _client.GetCurrentOpenOrders().NotNull()).NotNull().ToList();

                var openOrdersCount = _assets.Select(asset =>
                    {
                        var symbol = SymbolUtils.GetCurrencySymbol(asset, QuoteAsset);
                        var count = openOrders.Count(o => o.NotNull().Symbol == symbol);
                        return (symbol: symbol, count: count);
                    })
                    .ToDictionary(x => x.symbol, x => x.count);

                var amounts =
                    OrderDistributor.SplitIntoBuyOrders(freeQuoteBalance, MinOrderSize, openOrdersCount);

                Parallel.ForEach(
                    amounts,
                    new ParallelOptions {MaxDegreeOfParallelism = MaxDegreeOfParallelism},
                    symbolAmounts =>
                    {
                        try
                        {
                            var symbol = symbolAmounts.Key;
                            var price = GetActualPrice(symbol, OrderSide.Buy).Result;
                            var tradingRules = _rulesProvider.GetRulesFor(symbol);
                            var buyAmounts = symbolAmounts.Value.NotNull();
                            var profitStepSize = GetProfitStepSize(buyAmounts.Count);
                            var profitRatio = MinProfitRatio;

                            foreach (var quoteAmount in buyAmounts)
                            {
                                var buyPrice = AdjustPriceAccordingRules(price - price.Percents(profitRatio),
                                    tradingRules);
                                var baseAmount =
                                    OrderDistributor.GetFittingBaseAmount(quoteAmount, buyPrice, tradingRules.StepSize);
                                var orderRequest = new OrderRequest(symbol, OrderSide.Buy, baseAmount, buyPrice);

                                if (MeetsTradingRules(orderRequest))
                                {
                                    PlaceOrder(orderRequest).Wait();
                                    profitRatio += profitStepSize;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogException(ex);
                        }
                    });
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

        private static decimal GetProfitStepSize(int ordersCount)
        {
            var stepSize = ordersCount > 0 ? (MaxProfitRatio - MinProfitRatio) / ordersCount : 0;

            return stepSize.Round();
        }

        [NotNull]
        private OrderRequest CreateSellOrder([NotNull] IOrder message)
        {
            var tradingRules = _rulesProvider.GetRulesFor(message.Symbol);

            var sellPrice =
                AdjustPriceAccordingRules(message.Price + message.Price.Percents(MinProfitRatio), tradingRules);

            var orderRequest = new OrderRequest(message.Symbol, OrderSide.Sell, message.OrigQty, sellPrice);

            return orderRequest;
        }

        [NotNull]
        private OrderRequest CreateBuyOrder([NotNull] IOrder message)
        {
            var tradingRules = _rulesProvider.GetRulesFor(message.Symbol);

            var buyPrice =
                AdjustPriceAccordingRules(message.Price - message.Price.Percents(MinProfitRatio), tradingRules);

            var qty = AdjustQtyAccordingRules(message.Price * message.OrigQty / buyPrice, tradingRules);

            var orderRequest = new OrderRequest(message.Symbol, OrderSide.Buy, qty, buyPrice);
            return orderRequest;
        }

        private static decimal AdjustPriceAccordingRules(decimal price, [NotNull] ITradingRules rules)
        {
            return (int) (price / rules.TickSize) * rules.TickSize;
        }

        private static decimal AdjustQtyAccordingRules(decimal qty, [NotNull] ITradingRules rules)
        {
            return (int) (qty / rules.StepSize) * rules.StepSize;
        }
    }
}