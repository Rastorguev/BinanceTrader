using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

// ReSharper disable FunctionNeverReturns
namespace BinanceTrader.Trader
{
    public class RabbitTrader
    {
        private static readonly TimeSpan FundsCheckInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan OrdersCheckInterval = TimeSpan.FromMinutes(0.5);
        private static readonly TimeSpan DataStreamCheckInterval = TimeSpan.FromMinutes(1);

        private const decimal MinProfitRatio = 1m;
        private const decimal MaxProfitRatio = 1.1m;
        private const string QuoteAsset = "ETH";
        private const string FeeAsset = "BNB";
        private const decimal MinOrderSize = 0.015m;
        private readonly TimeSpan _newOrderExpiration = TimeSpan.FromMinutes(5);
        private string _listenKey;
        private IReadOnlyList<IBalance> _funds;

        [NotNull] private readonly IBinanceClient _client;
        [NotNull] private readonly ILogger _logger;
        [NotNull] private readonly TradingRulesProvider _rulesProvider;

        [NotNull] [ItemNotNull] private IReadOnlyList<string> _assets = new List<string>();
        [NotNull] private readonly FundsStateLogger _fundsStateLogger;

        public RabbitTrader(
            [NotNull] IBinanceClient client,
            [NotNull] ILogger logger)
        {
            _logger = logger;
            _client = client;
            _rulesProvider = new TradingRulesProvider(client);
            _fundsStateLogger = new FundsStateLogger(_client, _logger, QuoteAsset);
        }

        public async Task Start()
        {
            try
            {
                await _rulesProvider.UpdateRulesIfNeeded();
                _assets = _rulesProvider.GetBaseAssetsFor(QuoteAsset).Where(r => r != FeeAsset).ToList();
                _funds = (await _client.GetAccountInfo().NotNull().NotNull()).Balances.NotNull().ToList();

                StartCheckDataStream();
                //StartCheckOrders();
                StartCheckFunds();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private void StartCheckOrders()
        {
            Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {
                        await Task.WhenAll(CheckOrders(), Task.Delay(OrdersCheckInterval)).NotNull();
                    }
                },
                TaskCreationOptions.LongRunning);
        }

        private void StartCheckFunds()
        {
            Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {
                        await Task.WhenAll(CheckFunds(), Task.Delay(FundsCheckInterval)).NotNull();
                    }
                },
                TaskCreationOptions.LongRunning);
        }

        private void StartCheckDataStream()
        {
            Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {
                        await Task.WhenAll(KeepDataStreamAlive(), Task.Delay(DataStreamCheckInterval)).NotNull();
                    }
                },
                TaskCreationOptions.LongRunning);
        }

        private async Task CheckOrders()
        {
            try
            {
                await CancelExpiredOrders();
                await CheckFeeCurrency();
                await PlaceBuyOrders();
                await PlaceSellOrders();
            }

            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async Task CheckFunds()
        {
            try
            {
                await _rulesProvider.UpdateRulesIfNeeded();
                _assets = _rulesProvider.GetBaseAssetsFor(QuoteAsset).Where(r => r != FeeAsset).ToList();

                await _fundsStateLogger.LogFundsState(_funds.NotNull(), _assets);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
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

                var tradePrice = message.Price;

                switch (message.Side)
                {
                    case OrderSide.Buy:
                        var qty = message.OrigQty;
                        var sellRequest = CreateSellOrder(message, qty, tradePrice);
                        if (MeetsTradingRules(sellRequest))
                        {
                            await PlaceOrder(sellRequest);
                        }

                        break;

                    case OrderSide.Sell:
                        var quoteAmount = message.OrigQty * message.Price;
                        var buyRequest = CreateBuyOrder(message, quoteAmount, tradePrice);
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

        private void OnAccountInfoUpdated([NotNull] AccountUpdatedMessage message)
        {
            var funds = message.Balances.NotNull().ToList();
            Interlocked.Exchange(ref _funds, funds);
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

        private async Task ResetOrderUpdatesListening()
        {
            await StopListenDataStream();
            StartListenDataStream();
        }

        private void StartListenDataStream()
        {
            _listenKey = _client.ListenUserDataEndpoint(OnAccountInfoUpdated, OnTrade, m => { });
        }

        private async Task StopListenDataStream()
        {
            if (_listenKey != null)
            {
                await _client.CloseUserStream(_listenKey).NotNull();
                _listenKey = null;
            }
        }

        private async Task CheckFeeCurrency()
        {
            if (NeedToBuyFeeCurrency())
            {
                await BuyFeeCurrency();
            }
        }

        private async Task CancelExpiredOrders()
        {
            try
            {
                var openOrders = (await _client.GetCurrentOpenOrders().NotNull()).NotNull().ToList();
                var now = DateTime.Now;

                var expiredOrders = openOrders
                    .Where(o =>
                    {
                        var orderTime = o.NotNull().UnixTime.GetTime().ToLocalTime();

                        return now.ToLocalTime() - orderTime > _newOrderExpiration;
                    })
                    .ToList();

                var cancelTasks = expiredOrders.Select(
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
                var freeBalances =
                    _funds.NotNull().Where(b => b.NotNull().Free > 0 && _assets.Contains(b.Asset)).ToList();

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
                var freeQuoteBalance = _funds.NotNull()
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

        [NotNull]
        private OrderRequest CreateSellOrder([NotNull] IOrder message, decimal qty, decimal price)
        {
            var tradingRules = _rulesProvider.GetRulesFor(message.Symbol);

            var sellPrice =
                AdjustPriceAccordingRules(price + price.Percents(MinProfitRatio), tradingRules);

            var orderRequest =
                new OrderRequest(message.Symbol, OrderSide.Sell, qty, sellPrice);

            return orderRequest;
        }

        [NotNull]
        private OrderRequest CreateBuyOrder([NotNull] IOrder message, decimal quoteAmount, decimal price)
        {
            var tradingRules = _rulesProvider.GetRulesFor(message.Symbol);

            var buyPrice =
                AdjustPriceAccordingRules(price - price.Percents(MinProfitRatio), tradingRules);

            var qty = quoteAmount / buyPrice;
            var adjustedQty = AdjustQtyAccordingRules(qty, tradingRules);

            var orderRequest = new OrderRequest(message.Symbol, OrderSide.Buy, adjustedQty, buyPrice);

            return orderRequest;
        }

        private bool NeedToBuyFeeCurrency()
        {
            var feeAmount = _funds.NotNull().First(b => b.NotNull().Asset == FeeAsset).NotNull().Free;

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