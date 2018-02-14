using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Account;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using Binance.API.Csharp.Client.Models.WebSocket;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public class Trader
    {
        private readonly TimeSpan _scheduleInterval = TimeSpan.FromMinutes(10);
        private readonly TimeSpan _maxIdlePeriod = TimeSpan.FromHours(4);
        private const decimal ProfitRatio = 2m;
        private const decimal MinQuoteAmount = 0.01m;
        private const string QuoteAsset = "ETH";

        [NotNull] private readonly BinanceClient _binanceClient;
        [NotNull] [ItemNotNull] private readonly List<string> _symbols;
        [NotNull] private readonly Timer _timer;
        [NotNull] private readonly Logger _logger;

        public Trader(
            [NotNull] BinanceClient binanceClient,
            [NotNull] Logger logger,
            [NotNull] List<string> symbols)
        {
            _logger = logger;
            _binanceClient = binanceClient;
            _symbols = symbols;

            _timer = new Timer
            {
                Interval = _scheduleInterval.TotalMilliseconds,
                AutoReset = true
            };

            _timer.Elapsed += OnTimerElapsed;
        }

        public async void Start()
        {
            await CheckOrders();
            _timer.Start();
        }

        public async void OnTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            await CheckOrders();
        }

        private async Task CheckOrders()
        {
            foreach (var symbol in _symbols)
            {
                try
                {
                    _logger.LogTitle(symbol);

                    var now = DateTime.Now;
                    var order = await GetLastOrder(symbol).NotNull();
                    var isCompleted = order.Status == OrderStatus.Filled;
                    var isNew = order.Status == OrderStatus.New;
                    var isExpired = isNew && now.ToLocalTime() - order.GetTime().ToLocalTime() > _maxIdlePeriod;

                    _logger.LogOrder("Status", order);

                    if (isExpired)
                    {
                        _logger.LogImportant("CHECK EXPIRED");

                        await HandleExpiredOrder(order);
                    }
                    else if (isCompleted)
                    {
                        _logger.LogImportant("CHECK COMPLETED");

                        await HandleCompletedOrder(order);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(ex);
                }
            }

            try
            {
                var balance = await GetTotalBalance();
                _logger.Log($"Balance: {balance} {QuoteAsset}");
            }
            catch (Exception ex)
            {
                _logger.Log(ex);
            }

            _logger.LogSeparator();
        }

        private async Task HandleCompletedOrder([NotNull] IOrder order)
        {
            switch (order.Side)
            {
                case OrderSide.Sell:
                {
                    var amount = order.Price * order.OrigQty;
                    var price = (order.Price - order.Price.Percents(ProfitRatio)).Round();
                    var qty = Math.Floor(amount / price);

                    await TryPlaceOrder(OrderSide.Buy, order.Symbol, price, qty);
                    break;
                }
                case OrderSide.Buy:
                {
                    var price = (order.Price + order.Price.Percents(ProfitRatio)).Round();
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

        private async Task ReplaceWithActualPrice(IOrder order)
        {
            var priceInfo = await GetPrices(order.Symbol).NotNull();

            switch (order.Side)
            {
                case OrderSide.Buy:
                {
                    var amount = order.Price * order.OrigQty;
                    var price = priceInfo.AskPrice;
                    var qty = Math.Floor(amount / price);

                    await TryPlaceOrder(OrderSide.Buy, order.Symbol, price, qty);
                    break;
                }
                case OrderSide.Sell:
                {
                    var price = priceInfo.BidPrice;
                    var qty = order.OrigQty;

                    await TryPlaceOrder(OrderSide.Sell, order.Symbol, price, qty);
                    break;
                }
            }
        }

        private async Task<Order> GetLastOrder(string currency)
        {
            var lastOrder = (await _binanceClient.GetAllOrders(currency, null, 1)).NotNull().First();
            return lastOrder;
        }

        private async Task<Result<NewOrder>> TryPlaceOrder(OrderSide side, string symbol, decimal price, decimal qty)
        {
            var success = false;
            NewOrder newOrder = null;

            if (IsMinNotional(price, qty))
            {
                newOrder = await _binanceClient.PostNewOrder(
                    symbol,
                    qty,
                    price,
                    side);

                success = true;

                _logger.LogOrder("Placed", newOrder);
            }
            else
            {
                _logger.LogImportant($"{symbol} MIN NOTIONAL ERROR {price} * {qty} = {price * qty}");
            }

            return new Result<NewOrder>(success, newOrder);
        }

        private async Task<PriceChangeInfo> GetPrices(string symbol)
        {
            var priceInfo = (await _binanceClient.GetPriceChange24H(symbol)).NotNull().First();

            return priceInfo;
        }

        private Task<IEnumerable<Order>> GetOpenOrders()
        {
            return _binanceClient.GetCurrentOpenOrders();
        }

        private async Task<CanceledOrder> CancelOrder([NotNull] IOrder order)
        {
            var canceledOrder = await _binanceClient.CancelOrder(order.Symbol, order.OrderId);

            _logger.LogOrder("Canceled", order);

            return canceledOrder;
        }

        private async Task<decimal> GetTotalBalance()
        {
            var total = 0m;

            var prices = (await _binanceClient.GetAllPrices()).ToList();
            var balances = (await _binanceClient.GetAccountInfo()).NotNull()
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
                    total += assetTotal * prices.First(p => p.Symbol == symbol).Price;
                }
            }

            return total.Round();
        }

        private static bool IsMinNotional(decimal price, decimal qty)
        {
            var isMinNotional = price * qty >= MinQuoteAmount;

            return isMinNotional;
        }

        public class Result<T>
        {
            public bool Success { get; }
            public T Value { get; }

            public Result(bool success, T value)
            {
                Success = success;
                Value = value;
            }
        }

        //private async Task<decimal> GetFreeQuoteAmount()
        //{
        //    var freeQuoteAmount = (await _binanceClient.GetAccountInfo()).Balances
        //        .First(b => b.Asset == QuoteAsset).Free;

        //    return freeQuoteAmount;
        //}
    }
}