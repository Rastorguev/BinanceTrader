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
        private static readonly TimeSpan ExpiredOrdersCheckInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan FundsCheckInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan FreeAmountsCheckInterval = TimeSpan.FromMinutes(30);

        private const decimal MinProfitRatio = 1m;
        private const decimal MaxProfitRatio = 1.1m;
        private const string QuoteAsset = "ETH";
        private const string FeeAsset = "BNB";
        private const decimal MinOrderSize = 0.015m;
        private readonly TimeSpan _orderExpirationInterval = TimeSpan.FromMinutes(5);
        private bool _ignoreCanceledEvents;

        [NotNull] private readonly Timer _expiredOrdersCheckTimer = new Timer
        {
            Interval = ExpiredOrdersCheckInterval.TotalMilliseconds,
            AutoReset = true
        };

        [NotNull] private readonly Timer _fundsCheckTimer = new Timer
        {
            Interval = FundsCheckInterval.TotalMilliseconds,
            AutoReset = true
        };

        [NotNull] private readonly Timer _freeAmountsCheckTimer = new Timer
        {
            Interval = FreeAmountsCheckInterval.TotalMilliseconds,
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
            _freeAmountsCheckTimer.Elapsed += OnFreeAmountsCheckEvent;
            _expiredOrdersCheckTimer.Elapsed += OnExpiredOrdersCheckEvent;
        }

        public async void Start()
        {
            try
            {
                await _rulesProvider.UpdateRulesIfNeeded();
                _assets = _rulesProvider.GetBaseAssetsFor(QuoteAsset).Where(r => r != FeeAsset).ToList();
                _fundsStateChecker.Assets = _assets;

                await CheckFeeCurrency();
                await CheckOrders();
                await KeepDataStreamAlive();
                await _fundsStateChecker.LogFundsState();

                _expiredOrdersCheckTimer.Start();
                _freeAmountsCheckTimer.Start();
                _fundsCheckTimer.Start();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async void OnExpiredOrdersCheckEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                await KeepDataStreamAlive();               
                await CancelExpiredOrders();
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

        private async void OnFreeAmountsCheckEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                await _rulesProvider.UpdateRulesIfNeeded();
                _assets = _rulesProvider.GetBaseAssetsFor(QuoteAsset).Where(r => r != FeeAsset).ToList();
                _fundsStateChecker.Assets = _assets;

                await CheckFeeCurrency();
                await PlaceSellOrders();
                await PlaceBuyOrders();
            }

            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
            finally
            {
                _ignoreCanceledEvents = false;
            }
        }

        private async Task ResetOrderUpdatesListening()
        {
            await StopListenDataStream();
            StartListenDataStream();
        }

        private async Task KeepDataStreamAlive()
        {
            try
            {
                if (_listenKey != null)
                {
                    await _client.KeepAliveUserStream(_listenKey).NotNull();
                }
                else
                {
                    StartListenDataStream();
                }
            }
            catch (Exception)
            {
                await ResetOrderUpdatesListening();

                throw;
            }
        }

        private void StartListenDataStream()
        {
            _listenKey = _client.ListenUserDataEndpoint(m => { }, OnTrade, OnOrderUpdated);
        }

        private async Task StopListenDataStream()
        {
            if (_listenKey != null)
            {
                await _client.CloseUserStream(_listenKey).NotNull();
                _listenKey = null;
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

                _logger.LogOrderCompleted(message);

                var price = message.Price;

                switch (message.Side)
                {
                    case OrderSide.Buy:
                        var sellRequest = CreateSellOrder(message, price);
                        if (MeetsTradingRules(sellRequest))
                        {
                            await PlaceOrder(sellRequest);
                        }

                        break;

                    case OrderSide.Sell:
                        var buyRequest = CreateBuyOrder(message, price);
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

        private async void OnOrderUpdated([NotNull] OrderOrTradeUpdatedMessage message)
        {
            try
            {
                if (message.Status != OrderStatus.Canceled || _ignoreCanceledEvents)
                {
                    return;
                }

                var price = await GetActualPrice(message.Symbol, message.Side);

                switch (message.Side)
                {
                    case OrderSide.Buy:
                        var buyRequest = CreateBuyOrder(message, price);
                        if (MeetsTradingRules(buyRequest))
                        {
                            await PlaceOrder(buyRequest);
                        }

                        break;

                    case OrderSide.Sell:
                        var sellRequest = CreateSellOrder(message, price);
                        if (MeetsTradingRules(sellRequest))
                        {
                            await PlaceOrder(sellRequest);
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

        private async Task CheckFeeCurrency()
        {
            if (await NeedToBuyFeeCurrency())
            {
                try
                {
                    _ignoreCanceledEvents = true;
                    await Task.Delay(TimeSpan.FromSeconds(1)).NotNull();
                    await CancelBuyOrders();
                    await BuyFeeCurrency();
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex);
                }
                finally
                {
                    _ignoreCanceledEvents = false;
                }
            }
        }

        private async Task CancelExpiredOrders()
        {
            try
            {
                var openOrders = (await _client.GetCurrentOpenOrders().NotNull()).NotNull().ToList();
                var now = DateTime.Now;

                var expireOrders = openOrders
                    .Where(o => now.ToLocalTime() - o.NotNull().UnixTime.GetTime().ToLocalTime() >
                                _orderExpirationInterval)
                    .ToList();

                var cancelTasks = expireOrders.Select(
                    async order =>
                    {
                        try
                        {
                            await CancelOrder(order.NotNull());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogException(ex);
                        }
                    });

                await Task.WhenAll(cancelTasks).NotNull();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async Task CancelBuyOrders()
        {
            try
            {
                var openOrders = (await _client.GetCurrentOpenOrders().NotNull()).NotNull().ToList();
                var buyOrders = openOrders.Where(o => o.NotNull().Side == OrderSide.Buy);

                var cancelTasks = buyOrders.Select(
                    async order =>
                    {
                        try
                        {
                            await CancelOrder(order.NotNull());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogException(ex);
                        }
                    });

                await Task.WhenAll(cancelTasks).NotNull();
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

                var prices = (await _client.GetAllPrices().NotNull()).NotNull().ToList();
                var placeTasks = new List<Task>();

                foreach (var balance in freeBalances)
                {
                    try
                    {
                        var symbol =
                            SymbolUtils.GetCurrencySymbol(balance.NotNull().Asset.NotNull(), QuoteAsset);
                        var price = prices.First(p => p.NotNull().Symbol == symbol).NotNull().Price;
                        var tradingRules = _rulesProvider.GetRulesFor(symbol);

                        var sellAmounts =
                            OrderDistributor.SplitIntoSellOrders(
                                balance.Free,
                                MinOrderSize,
                                price,
                                tradingRules.StepSize);

                        var profitStepSize = GetProfitStepSize(sellAmounts.Count);
                        var profitRatio = MinProfitRatio;

                        var tasks = sellAmounts.Select(async amount =>
                        {
                            try
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
                            catch (Exception ex)
                            {
                                _logger.LogException(ex);
                            }
                        });

                        placeTasks.AddRange(tasks);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex);
                    }
                }

                await Task.WhenAll(placeTasks).NotNull();
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

                var prices = (await _client.GetAllPrices().NotNull()).NotNull().ToList();
                var placeTasks = new List<Task>();

                foreach (var symbolAmounts in amounts)
                {
                    try
                    {
                        var symbol = symbolAmounts.Key;
                        var price = prices.First(p => p.NotNull().Symbol == symbol).NotNull().Price;
                        var tradingRules = _rulesProvider.GetRulesFor(symbol);
                        var buyAmounts = symbolAmounts.Value.NotNull();
                        var profitStepSize = GetProfitStepSize(buyAmounts.Count);
                        var profitRatio = MinProfitRatio;

                        var tasks = buyAmounts.Select(async quoteAmount =>
                        {
                            try
                            {
                                var buyPrice = AdjustPriceAccordingRules(price - price.Percents(profitRatio),
                                    tradingRules);
                                var baseAmount =
                                    OrderDistributor.GetFittingBaseAmount(quoteAmount, buyPrice, tradingRules.StepSize);
                                var orderRequest = new OrderRequest(symbol, OrderSide.Buy, baseAmount, buyPrice);

                                if (MeetsTradingRules(orderRequest))
                                {
                                    await PlaceOrder(orderRequest);
                                    profitRatio += profitStepSize;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogException(ex);
                            }
                        });

                        placeTasks.AddRange(tasks);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex);
                    }
                }

                await Task.WhenAll(placeTasks).NotNull();
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

            _logger.LogOrderPlaced(newOrder);

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

            _logger.LogOrderCanceled(order);

            return canceledOrder;
        }

        private async Task<bool> NeedToBuyFeeCurrency()
        {
            var balance = (await _client.GetAccountInfo().NotNull()).NotNull().Balances.NotNull().ToList();
            var feeAmount = balance.First(b => b.NotNull().Asset == FeeAsset).NotNull().Free;

            return feeAmount < 1;
        }

        private async Task BuyFeeCurrency()
        {
            try
            {
                const int qty = 1;

                var feeSymbol = SymbolUtils.GetCurrencySymbol(FeeAsset, QuoteAsset);
                var price = await GetActualPrice(feeSymbol, OrderSide.Buy);

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
        private OrderRequest CreateSellOrder([NotNull] IOrder message, decimal price)
        {
            var tradingRules = _rulesProvider.GetRulesFor(message.Symbol);

            var sellPrice =
                AdjustPriceAccordingRules(price + price.Percents(MinProfitRatio), tradingRules);

            var orderRequest = new OrderRequest(message.Symbol, OrderSide.Sell, message.OrigQty, sellPrice);

            return orderRequest;
        }

        [NotNull]
        private OrderRequest CreateBuyOrder([NotNull] IOrder message, decimal price)
        {
            var tradingRules = _rulesProvider.GetRulesFor(message.Symbol);

            var buyPrice =
                AdjustPriceAccordingRules(price - price.Percents(MinProfitRatio), tradingRules);

            var qty = AdjustQtyAccordingRules(price * message.OrigQty / buyPrice, tradingRules);

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