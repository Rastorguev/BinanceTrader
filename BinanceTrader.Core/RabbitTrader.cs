using System.Globalization;
using BinanceApi.Domain.Interfaces;
using BinanceApi.Models.Account;
using BinanceApi.Models.Enums;
using BinanceApi.Models.Extensions;
using BinanceApi.Models.Market;
using BinanceApi.Models.Market.TradingRules;
using BinanceApi.Models.WebSocket;
using BinanceTrader.Tools;
using Polly;
using Polly.Retry;
using static System.Decimal;

// ReSharper disable FunctionNeverReturns
namespace BinanceTrader.Core;

public class RabbitTrader
{
    private const string FeeAsset = "BNB";

    private const string MaxStepExecutionTimeExceededError = "MaxStepExecutionTimeExceeded";
    private const decimal MinFeeAmount = 0.25m;

    private static readonly TimeSpan FundsAndTradingRulesCheckInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan OrdersCheckInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DataStreamCheckInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan MaxStreamEventsInterval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan VolatilityCheckInterval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MaxStepExecutionTime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan NonVolatileAssetsBuyOrderExpiration = TimeSpan.FromMinutes(30);

    private readonly IBinanceClient _client;

    private readonly FundsStateLogger _fundsStateLogger;

    private readonly ILogger _logger;

    private readonly OrderDistributor _orderDistributor;

    private readonly TimeSpan _orderExpiration;

    private readonly decimal _profitRatio;

    private readonly string _quoteAsset;

    private readonly TradingRulesProvider _rulesProvider;

    private readonly AsyncRetryPolicy _startRetryPolicy = Policy
        .Handle<Exception>(ex => !(ex is OperationCanceledException))
        .WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60)
        });

    private readonly VolatilityChecker _volatilityChecker;

    private IReadOnlyDictionary<string, IBalance> _funds = new Dictionary<string, IBalance>();

    private long _lastStreamEventTime = DateTime.Now.ToBinary();
    private string _apiListenKey;

    private IReadOnlyDictionary<string, decimal> _orderedVolatility = new Dictionary<string, decimal>();

    private IReadOnlyDictionary<string, decimal> _mostVolatileAssets = new Dictionary<string, decimal>();

    private IReadOnlyList<string> _tradingAssets = new List<string>();

    public RabbitTrader(
        IBinanceClient client,
        ILogger logger,
        TraderConfig config,
        VolatilityChecker volatilityChecker)
    {
        _quoteAsset = config.QuoteAsset;
        _orderExpiration = config.OrderExpiration;
        _profitRatio = config.ProfitRatio;

        _logger = logger;
        _volatilityChecker = volatilityChecker;
        _client = client;
        _rulesProvider = new TradingRulesProvider(client);
        _fundsStateLogger = new FundsStateLogger(_client, _logger, _quoteAsset);
        _orderDistributor = new OrderDistributor(_quoteAsset, _profitRatio, _rulesProvider, logger);
    }

    private DateTime LastStreamEventTime
    {
        get => DateTime.FromBinary(_lastStreamEventTime);
        set => Interlocked.Exchange(ref _lastStreamEventTime, value.ToBinary());
    }

    public async Task Start()
    {
        try
        {
            await _startRetryPolicy.ExecuteAsync(async () =>
            {
                await UpdateFundsAndTradingRules();

                StartCheckDataStream();
                StartCheckOrders();
                StartUpdateFundsAndTradingRules();
                StartCheckVolatility();
            });
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
    }

    private void StartCheckOrders()
    {
        StartRepeatableStep("CheckOrders", CheckOrders, OrdersCheckInterval);
    }

    private void StartUpdateFundsAndTradingRules()
    {
        StartRepeatableStep("UpdateFundsAndTradingRules", UpdateFundsAndTradingRules,
            FundsAndTradingRulesCheckInterval);
    }

    private void StartCheckVolatility()
    {
        StartRepeatableStep("CheckVolatility", CheckVolatility, VolatilityCheckInterval);
    }

    private void StartCheckDataStream()
    {
        StartRepeatableStep("CheckDataStream", CheckDataStream, DataStreamCheckInterval);
    }

    private void StartRepeatableStep(string name, Func<Task> stepProvider, TimeSpan interval)
    {
        Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    var delayTask = Task.Delay(MaxStepExecutionTime);
                    var stepTask = Task.WhenAny(stepProvider(), delayTask);
                    if (delayTask.IsCompleted)
                    {
                        _logger.LogMessage(MaxStepExecutionTimeExceededError,
                            $"{name} takes more than {MaxStepExecutionTime}");
                    }

                    await Task.WhenAll(stepTask, Task.Delay(interval));
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
            await PlaceSellOrders();
            await PlaceBuyOrders();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
    }

    private async Task UpdateFundsAndTradingRules()
    {
        try
        {
            await _rulesProvider.UpdateRulesIfNeeded();
            _tradingAssets = GetTradingAssets();
            var newFunds = (await _client.GetAccountInfo())
                .Balances
                .ToDictionary(x => x.Asset, x => x);

            Interlocked.Exchange(ref _funds, newFunds);

            await _fundsStateLogger.LogFundsState(_funds.Values.ToList(), _tradingAssets);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
    }

    private async void OnTrade(OrderOrTradeUpdatedMessage message)
    {
        try
        {
            LastStreamEventTime = DateTime.Now;

            var baseAsset = SymbolUtils.GetBaseAsset(message.Symbol, _quoteAsset);

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
                    var qty = message.OrderQuantity;
                    var sellRequest = CreateSellOrder(message, qty, tradePrice);
                    if (MeetsTradingRules(sellRequest))
                    {
                        await PlaceOrder(sellRequest);
                    }

                    break;

                case OrderSide.Sell:
                    var quoteAmount = message.OrderQuantity * message.Price;
                    var buyRequest = CreateBuyOrder(message, quoteAmount, tradePrice);
                    if (MeetsTradingRules(buyRequest))
                    {
                        await PlaceOrder(buyRequest);
                    }

                    break;

                case OrderSide.Unknown:
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
    }

    private void OnAccountInfoUpdated(AccountUpdatedMessage message)
    {
        try
        {
            LastStreamEventTime = DateTime.Now;

            var updatedFunds = message.Balances.ToList();
            var newFunds = _funds.ToDictionary(x => x.Key, x => x.Value);

            foreach (var f in updatedFunds)
            {
                newFunds[f.Asset] = f;
            }

            Interlocked.Exchange(ref _funds, newFunds);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
    }

    private async Task KeepDataStreamAlive()
    {
        try
        {
            if (_apiListenKey != null)
            {
                await _client.KeepAliveUserStream(_apiListenKey);
                _logger.LogMessage("KeepAlive", $"{_apiListenKey}");
            }
            else
            {
                StartListenDataStream();
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);

            ResetOrderUpdatesListening();
        }
    }

    private async void ResetOrderUpdatesListening()
    {
        try
        {
            await StopListenDataStream();
            StartListenDataStream();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
    }

    private void StartListenDataStream()
    {
        _apiListenKey = _client.ListenUserDataEndpoint(OnAccountInfoUpdated, OnTrade, _ => { });
        LastStreamEventTime = DateTime.Now;

        _logger.LogMessage("StartListen", $"{_apiListenKey}");
    }

    private async Task StopListenDataStream()
    {
        try
        {
            if (_apiListenKey != null)
            {
                _logger.LogMessage("StopListenListen", $"{_apiListenKey}");
                await _client.CloseUserStream(_apiListenKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
        finally
        {
            _apiListenKey = null;
        }
    }

    private async Task CheckDataStream()
    {
        if (LastStreamEventTime.Add(MaxStreamEventsInterval) < DateTime.Now)
        {
            _logger.LogMessage("ResetDataStream", $"{_apiListenKey}");
            ResetOrderUpdatesListening();
        }
        else
        {
            await KeepDataStreamAlive();
        }
    }

    private async Task CheckFeeCurrency()
    {
        try
        {
            if (NeedToBuyFeeCurrency())
            {
                await BuyFeeCurrency();
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
    }

    private async Task CancelExpiredOrders()
    {
        try
        {
            var openOrders = (await _client.GetCurrentOpenOrders()).ToList();
            var now = DateTime.Now.ToLocalTime();
            var nonVolatileAssetsBuyOrderExpiration =
                new TimeSpan(Math.Min(_orderExpiration.Ticks, NonVolatileAssetsBuyOrderExpiration.Ticks));

            var expiredOrders = openOrders
                .Where(o =>
                {
                    var orderTime = o.UnixTime.GetTime().ToLocalTime();

                    return now - orderTime > _orderExpiration;
                })
                .ToList();

            var nonVolatileAssetsBuyOrders = openOrders
                .Where(o =>
                {
                    var orderTime = o.UnixTime.GetTime().ToLocalTime();
                    var baseAsset = SymbolUtils.GetBaseAsset(o.Symbol, _quoteAsset);
                    var isInMostVolatileAssets =
                        !_mostVolatileAssets.Any() || _mostVolatileAssets.ContainsKey(baseAsset);
                    var shouldCancel =
                        o.Side == OrderSide.Buy &&
                        !isInMostVolatileAssets &&
                        now - orderTime > nonVolatileAssetsBuyOrderExpiration;

                    return shouldCancel;
                })
                .ToList();

            var ordersToCancel = expiredOrders
                .Concat(nonVolatileAssetsBuyOrders)
                .DistinctBy(x => x.OrderId)
                .ToList();

            var cancelTasks = ordersToCancel
                .Where(o => _rulesProvider.GetRulesFor(o.Symbol).Status == SymbolStatus.Trading)
                .Select(
                    async order =>
                    {
                        try
                        {
                            await CancelOrder(order);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogException(ex);
                        }
                    });

            await Task.WhenAll(cancelTasks);
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
                _funds.Values.ToList()
                    .Where(b => b.Free > 0 && _tradingAssets.Contains(b.Asset)).ToList();

            var prices = (await _client.GetAllPrices()).ToList();
            var placeTasks = new List<Task>();

            foreach (var balance in freeBalances)
            {
                try
                {
                    var symbol =
                        SymbolUtils.GetCurrencySymbol(balance.Asset, _quoteAsset);

                    var tradingRules = _rulesProvider.GetRulesFor(symbol);
                    if (tradingRules.Status != SymbolStatus.Trading)
                    {
                        continue;
                    }

                    var price = prices.First(p => p.Symbol == symbol).Price;
                    var sellPrice =
                        RulesHelper.GetMaxFittingPrice(price + price.Percentage(_profitRatio), tradingRules);

                    var minNotionalQty = RulesHelper.GetMinNotionalQty(price, tradingRules);
                    var maxFittingQty = RulesHelper.GetMaxFittingQty(balance.Free, tradingRules);

                    if (maxFittingQty >= minNotionalQty)
                    {
                        var orderRequest =
                            new OrderRequest(symbol, OrderSide.Sell, maxFittingQty, sellPrice);

                        if (MeetsTradingRules(orderRequest))
                        {
                            var task = Task.Factory.StartNew(async () =>
                            {
                                try
                                {
                                    await PlaceOrder(orderRequest);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogException(ex);
                                }
                            });

                            placeTasks.Add(task);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex);
                }
            }

            await Task.WhenAll(placeTasks);
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
            var freeQuoteBalance = _funds.Values
                .First(b => b.Asset == _quoteAsset).Free;

            var openOrders = (await _client.GetCurrentOpenOrders()).ToList();
            var prices = (await _client.GetAllPrices()).ToList();
            var assetsToBuy = _mostVolatileAssets.Any()
                ? _mostVolatileAssets.Select(x => x.Key).ToList()
                : _tradingAssets;
            var orderRequests =
                _orderDistributor.SplitIntoBuyOrders(freeQuoteBalance, assetsToBuy, openOrders, prices);

            var placeTasks = orderRequests.Select(async r =>
            {
                try
                {
                    if (MeetsTradingRules(r))
                    {
                        await PlaceOrder(r);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex);
                }
            });

            await Task.WhenAll(placeTasks);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
    }

    private async Task<decimal> GetActualPrice(string symbol, OrderSide orderSide)
    {
        var priceInfo = await GetPrices(symbol);

        switch (orderSide)
        {
            case OrderSide.Buy:
                return priceInfo.AskPrice;
            case OrderSide.Sell:
                return priceInfo.BidPrice;
            case OrderSide.Unknown:
                return priceInfo.AskPrice;
            default:
                return priceInfo.AskPrice;
        }
    }

    private async Task<NewOrder> PlaceOrder(OrderRequest orderRequest,
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
                )
            ;

        _logger.LogOrderPlaced(newOrder);

        return newOrder;
    }

    private async Task CancelOrder(IOrder order)
    {
        await _client.CancelOrder(order.Symbol, order.OrderId);

        _logger.LogOrderCanceled(order);
    }

    private bool MeetsTradingRules(OrderRequest orderRequest)
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
        var priceInfo = (await _client.GetPriceChange24H(symbol)).First();

        return priceInfo;
    }

    private OrderRequest CreateSellOrder(IOrder message, decimal qty, decimal price)
    {
        var tradingRules = _rulesProvider.GetRulesFor(message.Symbol);
        var sellPrice = RulesHelper.GetMaxFittingPrice(price + price.Percentage(_profitRatio), tradingRules);
        var orderRequest =
            new OrderRequest(message.Symbol, OrderSide.Sell, qty, sellPrice);

        return orderRequest;
    }

    private OrderRequest CreateBuyOrder(IOrder message, decimal quoteAmount, decimal price)
    {
        var tradingRules = _rulesProvider.GetRulesFor(message.Symbol);
        var buyPrice = RulesHelper.GetMaxFittingPrice(price - price.Percentage(_profitRatio), tradingRules);
        var qty = quoteAmount / buyPrice;
        var adjustedQty = RulesHelper.GetMaxFittingQty(qty, tradingRules);
        var orderRequest = new OrderRequest(message.Symbol, OrderSide.Buy, adjustedQty, buyPrice);

        return orderRequest;
    }

    private bool NeedToBuyFeeCurrency()
    {
        var feeAmount = _funds[FeeAsset].Free;

        return feeAmount < MinFeeAmount;
    }

    private async Task BuyFeeCurrency()
    {
        var feeSymbol = SymbolUtils.GetCurrencySymbol(FeeAsset, _quoteAsset);
        var price = await GetActualPrice(feeSymbol, OrderSide.Buy);

        var orderRequest = new OrderRequest(feeSymbol, OrderSide.Buy, MinFeeAmount, price);

        if (MeetsTradingRules(orderRequest))
        {
            var order = await PlaceOrder(orderRequest, OrderType.Market, TimeInForce.IOC);
            var status = order.Status;
            var executedQty = order.ExecutedQty;

            _logger.LogMessage("BuyFeeCurrency",
                $"Status {status}, Quantity {executedQty}, Price {price}");
        }
    }

    private async Task CheckVolatility()
    {
        try
        {
            await UpdateVolatility();

            LogVolatility(_orderedVolatility);
            LogMostVolatileAssets(_mostVolatileAssets);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
    }

    private async Task UpdateVolatility()
    {
        var orderedVolatility = new Dictionary<string, decimal>();
        var mostVolatileAssets = new Dictionary<string, decimal>();

        try
        {
            orderedVolatility = (await _volatilityChecker.GetAssetsVolatility(
                    _tradingAssets,
                    _quoteAsset,
                    DateTime.Now - VolatilityCheckInterval,
                    DateTime.Now,
                    TimeInterval.Minutes_1))
                .OrderByDescending(x => x.Value)
                .ToList()
                .ToDictionary(x => x.Key, x => x.Value);

            var volatileAssets = orderedVolatility
                .Where(x => x.Value > 0)
                .ToDictionary(x => x.Key, x => x.Value);

            mostVolatileAssets = volatileAssets
                .OrderByDescending(x => x.Value)
                .Take(Math.Min(volatileAssets.Count, orderedVolatility.Count / 2))
                .ToDictionary(x => x.Key, x => x.Value);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
        finally
        {
            if (orderedVolatility.Any())
            {
                Interlocked.Exchange(ref _orderedVolatility, orderedVolatility);
                Interlocked.Exchange(ref _mostVolatileAssets, mostVolatileAssets);
            }
        }
    }

    private void LogVolatility(IReadOnlyDictionary<string, decimal> volatility)
    {
        var median = volatility.Select(v => v.Value).Median();
        var average = volatility.Select(v => v.Value).Average();

        _logger.LogMessage("Volatility", new Dictionary<string, string>
        {
            { "Median", median.ToString(CultureInfo.InvariantCulture) },
            { "Average", average.ToString(CultureInfo.InvariantCulture) }
        });
    }

    private void LogMostVolatileAssets(IReadOnlyDictionary<string, decimal> mostVolatileAssets)
    {
        _logger.LogMessage("MostVolatileAssets",
            string.Join(",\n", mostVolatileAssets.Select(x => string.Join(" - ", x.Key, x.Value.Round4()))));
    }

    private List<string> GetTradingAssets()
    {
        var assets = _rulesProvider.GetBaseAssetsFor(_quoteAsset).Where(r => r != FeeAsset).ToList();

        return assets;
    }
}