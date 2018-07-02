using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using Timer = System.Timers.Timer;

namespace BinanceTrader.Trader
{
    public class RabbitTrader
    {
        private static readonly TimeSpan FundsCheckInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan OrdersCheckInterval = TimeSpan.FromMinutes(0.5);

        private const decimal MinProfitRatio = 1m;
        private const decimal MaxProfitRatio = 1.1m;
        private const string QuoteAsset = "ETH";
        private const string FeeAsset = "BNB";
        private const decimal MinOrderSize = 0.015m;
        private readonly TimeSpan _newOrderExpiration = TimeSpan.FromMinutes(5);

        [NotNull] private readonly Timer _fundsCheckTimer = new Timer
        {
            Interval = FundsCheckInterval.TotalMilliseconds,
            AutoReset = true
        };

        [NotNull] private readonly IBinanceClient _client;
        [NotNull] private readonly ILogger _logger;
        [NotNull] private readonly TradingRulesProvider _rulesProvider;

        [NotNull] [ItemNotNull] private IReadOnlyList<string> _assets = new List<string>();
        [NotNull] private readonly FundsStateChecker _fundsStateChecker;

        public RabbitTrader(
            [NotNull] IBinanceClient client,
            [NotNull] ILogger logger)
        {
            _logger = logger;
            _client = client;
            _rulesProvider = new TradingRulesProvider(client);
            _fundsStateChecker = new FundsStateChecker(_client, _logger, QuoteAsset);

            _fundsCheckTimer.Elapsed += OnFundsEvent;
        }

        public async void Start()
        {
            while (true)
            {
                try
                {
                    await _rulesProvider.UpdateRulesIfNeeded();
                    _assets = _rulesProvider.GetBaseAssetsFor(QuoteAsset).Where(r => r != FeeAsset).ToList();
                    _fundsStateChecker.Assets = _assets;

                    await CancelExpiredOrders();
                    await CheckFeeCurrency();
                    await PlaceSellOrders();
                    await PlaceBuyOrders();
                    await _fundsStateChecker.LogFundsState();

                    _fundsCheckTimer.Start();
                    StartCheckOrders();
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex);
                }
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

                    // ReSharper disable FunctionNeverReturns
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            // ReSharper restore FunctionNeverReturns
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

        private async Task CheckOrders()
        {
            try
            {
                await _rulesProvider.UpdateRulesIfNeeded();
                _assets = _rulesProvider.GetBaseAssetsFor(QuoteAsset).Where(r => r != FeeAsset).ToList();
                _fundsStateChecker.Assets = _assets;

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

        private async Task CheckFeeCurrency()
        {
            if (await NeedToBuyFeeCurrency())
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

        private static decimal AdjustPriceAccordingRules(decimal price, [NotNull] ITradingRules rules)
        {
            return (int) (price / rules.TickSize) * rules.TickSize;
        }
    }
}