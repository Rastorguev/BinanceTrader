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
using Binance.API.Csharp.Client.Models.WebSocket;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader.Trader
{
    public class RabbitTrader
    {
        private readonly TimeSpan _scheduleInterval = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _maxIdlePeriod = TimeSpan.FromHours(12);
        private const decimal ProfitRatio = 2m;
        private const string QuoteAsset = "ETH";
        private const string FeeAsset = "BNB";
        private const string UsdtAsset = "USDT";

        [NotNull] private readonly IBinanceClient _client;
        [NotNull] private readonly Timer _timer;
        [NotNull] private readonly ILogger _logger;
        [NotNull] private readonly TradingRulesProvider _rulesProvider;

        [NotNull] [ItemNotNull] private readonly List<string> _assets =
            new List<string>
            {
                "NCASH",
                "IOST",
                "STORM",
                "TRX",
                "FUN",
                "POE",
                "TNB",
                "XVG",
                "CDT",
                "DNT",
                "LEND",
                "MANA",
                "SNGLS",
                "TNT",
                "FUEL",
                "YOYO",
                "CND",
                "RCN",
                "MTH",
                "CMT",
                "SNT",
                "RPX",
                "ENJ",
                "CHAT",
                "BTS",
                "VIB",
                "SNM",
                "OST",
                "QSP",
                "DLT",
                "BAT"
            };

        public RabbitTrader(
            [NotNull] IBinanceClient client,
            [NotNull] ILogger logger)
        {
            _logger = logger;
            _client = client;
            _rulesProvider = new TradingRulesProvider(client);

            _timer = new Timer
            {
                Interval = _scheduleInterval.TotalMilliseconds,
                AutoReset = true
            };

            _timer.Elapsed += OnTimerElapsed;
        }

        public async void Start()
        {
            await ExecuteScheduledTasks();

            _timer.Start();
        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            await ExecuteScheduledTasks();
        }

        public async Task ExecuteScheduledTasks()
        {
            try
            {
                await _rulesProvider.UpdateRulesIfNeeded();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }

            await CheckOrders();

            try
            {
                await BuyFeeCurrencyIfNeeded();
                await LogCurrentBalance();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private async Task CheckOrders()
        {
            foreach (var asset in _assets)
            {
                try
                {
                    var symbol = GetCurrencySymbol(asset, QuoteAsset);
                    var order = await GetLastOrder(symbol).NotNull();

                    if (order == null)
                    {
                        _logger.LogWarning("NoOrdersFound", $"No orders for {symbol}");
                        continue;
                    }

                    var now = DateTime.Now;
                    var isCompleted = order.Status == OrderStatus.Filled;
                    var isNew = order.Status == OrderStatus.New;
                    var isExpired =
                        isNew && now.ToLocalTime() - order.UnixTime.GetTime().ToLocalTime() > _maxIdlePeriod;

                    if (isExpired)
                    {
                        await HandleExpiredOrder(order);
                    }
                    else if (isCompleted)
                    {
                        await HandleCompletedOrder(order);
                        _logger.LogOrder("Completed", order);
                    }
                    else if (order.Status != OrderStatus.New &&
                             order.Status != OrderStatus.Filled)
                    {
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex);
                }
            }
        }

        private async Task HandleCompletedOrder([NotNull] IOrder order)
        {
            switch (order.Side)
            {
                case OrderSide.Sell:
                {
                    var amount = order.Price * order.OrigQty;
                    var price = (order.Price - order.Price.Percents(ProfitRatio)).Round();
                    if (price == 0)
                    {
                        price = await GetActualPrice(order.Symbol, OrderSide.Buy);
                    }

                    var qty = Math.Floor(amount / price);

                    await TryPlaceOrder(OrderSide.Buy, order.Symbol, price, qty);
                    break;
                }
                case OrderSide.Buy:
                {
                    var price = (order.Price + order.Price.Percents(ProfitRatio)).Round();
                    if (price == 0)
                    {
                        price = await GetActualPrice(order.Symbol, OrderSide.Sell);
                    }

                    var qty = order.OrigQty;

                    await TryPlaceOrder(OrderSide.Sell, order.Symbol, price, qty);
                    break;
                }
            }
        }

        private async Task HandleExpiredOrder([NotNull] IOrder order)
        {
            await CancelOrder(order);

            await ReplaceWithActualPrice(order);
        }

        private async Task ReplaceWithActualPrice([NotNull] IOrder order)
        {
            switch (order.Side)
            {
                case OrderSide.Buy:
                {
                    var amount = order.Price * order.OrigQty;
                    var price = await GetActualPrice(order.Symbol, OrderSide.Buy);
                    var qty = Math.Floor(amount / price);

                    await TryPlaceOrder(OrderSide.Buy, order.Symbol, price, qty);
                    break;
                }
                case OrderSide.Sell:
                {
                    var price = await GetActualPrice(order.Symbol, OrderSide.Sell);
                    var qty = order.OrigQty;

                    await TryPlaceOrder(OrderSide.Sell, order.Symbol, price, qty);
                    break;
                }
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

        private async Task<Order> GetLastOrder(string currency)
        {
            var lastOrder = (await _client.GetAllOrders(currency, null, 1).NotNull()).NotNull().FirstOrDefault();

            return lastOrder;
        }

        private async Task<Result<NewOrder>> TryPlaceOrder(
            OrderSide side,
            string symbol,
            decimal price,
            decimal qty,
            TimeInForce timeInForce = TimeInForce.GTC)
        {
            var success = false;
            NewOrder newOrder = null;

            if (IsMinNotional(symbol, price, qty))
            {
                newOrder = (await _client.PostNewOrder(
                            symbol,
                            qty,
                            price,
                            side,
                            timeInForce: timeInForce)
                        .NotNull())
                    .NotNull();

                success = true;

                _logger.LogOrder("Placed", newOrder);
            }
            else
            {
                _logger.LogWarning("MinNotionalError", $"{symbol} : {price} * {qty} = {price * qty}");
            }

            return new Result<NewOrder>(success, newOrder);
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

        private async Task LogCurrentBalance()
        {
            var total = 0m;

            var prices = (await _client.GetAllPrices().NotNull().NotNull()).ToList();
            var balances = (await _client.GetAccountInfo().NotNull()).NotNull()
                .Balances.NotNull().Where(b => b.NotNull().Free + b.NotNull().Locked > 0).ToList();

            foreach (var balance in balances)
            {
                var assetTotal = balance.NotNull().Free + balance.NotNull().Locked;

                if (balance.NotNull().Asset == QuoteAsset)
                {
                    total += assetTotal;
                }
                else
                {
                    var symbol = $"{balance.NotNull().Asset}{QuoteAsset}";
                    total += assetTotal * prices.First(p => p.NotNull().Symbol == symbol).NotNull().Price;
                }
            }

            var ethUsdt = GetCurrencySymbol(QuoteAsset, UsdtAsset);
            var balanceUsdt = total * prices.First(p => p.NotNull().Symbol == ethUsdt).NotNull().Price;

            _logger.LogMessage("Balance",
                $"{Math.Round(total, 3)} {QuoteAsset} ({Math.Round(balanceUsdt, 3)} {UsdtAsset})");
        }

        private bool IsMinNotional(string symbol, decimal price, decimal qty)
        {
            var minNotional = _rulesProvider.GetRulesFor(symbol).MinNotional;
            var isMinNotional = price * qty >= minNotional;

            return isMinNotional;
        }

        private async Task BuyFeeCurrencyIfNeeded()
        {
            const int qty = 1;
            var feeSymbol = GetCurrencySymbol(FeeAsset, QuoteAsset);

            var lastOrder = await GetLastOrder(feeSymbol).NotNull();
            if (lastOrder.Status == OrderStatus.New ||
                lastOrder.Status == OrderStatus.PartiallyFilled)
            {
                await CancelOrder(lastOrder);
            }

            var balance = (await _client.GetAccountInfo().NotNull()).NotNull().Balances.NotNull().ToList();

            var feeAmmount = balance.First(b => b.NotNull().Asset == FeeAsset).NotNull().Free;
            var qouteAmmount = balance.First(b => b.NotNull().Asset == QuoteAsset).NotNull().Free;
            var price = await GetActualPrice(feeSymbol, OrderSide.Buy);

            if (feeAmmount < 1 && qouteAmmount >= price * qty)
            {
                var result = await TryPlaceOrder(OrderSide.Buy, feeSymbol, price, qty, TimeInForce.IOC).NotNull();
                if (result.Value != null)
                {
                    var status = result.Value.Status;
                    var executedQty = result.Value.ExecutedQty;

                    _logger.LogMessage("BuyFeeCurrency",
                        $"Status {status}, Quantity {executedQty}, Price {price}");
                }
            }
        }

        private static string GetCurrencySymbol(string originalAsset, string quoteAsset)
        {
            return string.Format($"{originalAsset}{quoteAsset}");
        }
    }
}