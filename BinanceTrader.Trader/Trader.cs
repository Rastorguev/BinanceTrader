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

        [NotNull] private readonly BinanceClient _binanceClient;
        [NotNull] [ItemNotNull] private readonly List<string> _currencies;
        [NotNull] private readonly Timer _timer;
        [NotNull] private readonly Logger _logger;

        public Trader(
            [NotNull] BinanceClient binanceClient,
            [NotNull] Logger logger,
            [NotNull] List<string> currencies)
        {
            _logger = logger;
            _binanceClient = binanceClient;
            _currencies = currencies;

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

        //private async void OnOrderChanged([NotNull] OrderOrTradeUpdatedMessage order)
        //{
        //    await ExecuteSafe(async () =>
        //    {
        //        if (order.Status == OrderStatus.Filled)
        //        {
        //            _logger.LogOrder("Completed", order, "ChangedEvent");

        //            var hasOpenOrder = (await GetOpenOrders()).Any(o => o.Symbol == order.Symbol);
        //            if (!hasOpenOrder)
        //            {
        //                var newOrder = await PlaceOppositeOrder(order);
        //                _logger.LogOrder("Placed", newOrder, "ChangedEvent");
        //            }
        //            else
        //            {
        //                _logger.LogOrder("OrderAlreadyExists", order, "ChangedEvent");
        //            }
        //        }
        //    });
        //}

        private async Task CheckOrders()
        {
            foreach (var currency in _currencies)
            {
                var now = DateTime.Now;
                var order = await GetLastOrder(currency).NotNull();
                var isCompleted = order.Status == OrderStatus.Filled;
                var isNew = order.Status == OrderStatus.New;
                var isOutdated = isNew && now.ToLocalTime() - order.GetTime().ToLocalTime() > _maxIdlePeriod;

                if (isOutdated)
                {
                    await HandleExpiredOrder(order);
                }
                else if (isCompleted)
                {
                    await HandleCompletedOrder(order);
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
                    var qty = Math.Floor(amount / price);

                    if (IsMinNotional(price, qty))
                    {
                        await PlaceOrder(OrderSide.Buy, order.Symbol, price, qty);
                    }

                    break;
                }
                case OrderSide.Buy:
                {
                    var price = (order.Price + order.Price.Percents(ProfitRatio)).Round();
                    var qty = order.OrigQty;

                    if (IsMinNotional(price, qty))
                    {
                        await PlaceOrder(OrderSide.Sell, order.Symbol, price, qty);
                    }

                    break;
                }
            }
        }

        private async Task HandleExpiredOrder([NotNull] IOrder order)
        {
            await CancelOrder(order);
            _logger.LogOrder("Canceled", order, "CheckOutdated");

            var priceInfo = await GetPriceInfo(order.Symbol).NotNull();

            switch (order.Side)
            {
                case OrderSide.Buy:
                {
                    var amount = order.Price * order.OrigQty;
                    var price = priceInfo.AskPrice;
                    var qty = Math.Floor(amount / price);

                    if (IsMinNotional(price, qty))
                    {
                        var newOrder = await PlaceOrder(OrderSide.Buy, order.Symbol, price, qty);
                        _logger.LogOrder("Placed", newOrder, "CheckOutdated");
                    }

                    break;
                }
                case OrderSide.Sell:
                {
                    var price = priceInfo.BidPrice;
                    var qty = order.OrigQty;

                    if (IsMinNotional(price, qty))
                    {
                        var newOrder = await PlaceOrder(OrderSide.Sell, order.Symbol, price, qty);
                        _logger.LogOrder("Placed", newOrder, "CheckOutdated");
                    }

                    break;
                }
            }
        }

        private bool IsMinNotional(decimal price, decimal qty)
        {
            var isMinNotional = price * qty >= MinQuoteAmount;

            if (!isMinNotional)
            {
                _logger.Log($"MIN_NOTIONAL {price} * {qty}");
            }

            return isMinNotional;
        }

        private async Task<PriceChangeInfo> GetPriceInfo(string symbol)
        {
            var priceInfo = (await _binanceClient.GetPriceChange24H(symbol)).NotNull().First();

            return priceInfo;
        }

        private Task<IEnumerable<Order>> GetOpenOrders()
        {
            return _binanceClient.GetCurrentOpenOrders();
        }

        //private async Task CheckCompletedOrders()
        //{
        //    foreach (var currency in _currencies)
        //    {
        //        var lastOrder = await GetLastOrder(currency).NotNull();
        //        if (lastOrder.Status == OrderStatus.Filled)
        //        {
        //            var newOrder = await PlaceOppositeOrder(lastOrder);
        //            _logger.LogOrder("Placed", newOrder, "CheckCompleted");
        //        }
        //    }

        //    _logger.Log("CheckCompletedOrders");
        //}

        //private async Task<IOrder> PlaceOppositeOrder(IOrder order)
        //{
        //    switch (order.Side)
        //    {
        //        case OrderSide.Sell:
        //        {
        //            var amount = order.Price * order.OrigQty;
        //            var price = (order.Price - order.Price.Percents(ProfitRatio)).Round();
        //            var qty = Math.Floor(amount / price);

        //            if (amount > MinQuoteAmount && qty > 0)
        //            {
        //                return await PlaceOrder(OrderSide.Buy, order.Symbol, price, qty);
        //            }

        //            _logger.Log($"Insufficient balance {order.Symbol}");

        //            break;
        //        }
        //        case OrderSide.Buy:
        //        {
        //            var price = (order.Price + order.Price.Percents(ProfitRatio)).Round();

        //            return await PlaceOrder(OrderSide.Sell, order.Symbol, price, order.OrigQty);
        //        }
        //    }

        //    return null;
        //}

        private async Task<Order> GetLastOrder(string currency)
        {
            var lastOrder = (await _binanceClient.GetAllOrders(currency, null, 1)).NotNull().First();
            return lastOrder;
        }

        private async Task<NewOrder> PlaceOrder(OrderSide side, string symbol, decimal price, decimal qty)
        {
            var newOrder = await _binanceClient.PostNewOrder(
                symbol,
                qty,
                price,
                side);

            return newOrder;
        }

        private async Task<CanceledOrder> CancelOrder([NotNull] IOrder order)
        {
            var canceledOrder = await _binanceClient.CancelOrder(order.Symbol, order.OrderId);

            return canceledOrder;
        }

        //private async Task<decimal> GetFreeQuoteAmount()
        //{
        //    var freeQuoteAmount = (await _binanceClient.GetAccountInfo()).Balances
        //        .First(b => b.Asset == QuoteAsset).Free;

        //    return freeQuoteAmount;
        //}
    }
}