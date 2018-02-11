using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Account;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.WebSocket;
using BinanceTrader.Tools;
using JetBrains.Annotations;
using Timer = System.Timers.Timer;

namespace BinanceTrader
{
    public class Trader
    {
        private readonly TimeSpan _scheduleInterval = TimeSpan.FromMinutes(0.5);
        private readonly TimeSpan _maxIdlePeriod = TimeSpan.FromMinutes(2);
        private const decimal ProfitRatio = 0.05m;
        private const decimal MinQuoteAmount = 0.01m;

        [NotNull] private readonly BinanceClient _binanceClient;
        [NotNull] [ItemNotNull] private readonly List<string> _currencies;
        [CanBeNull] private string _listenKey;
        [NotNull] private readonly Timer _timer;
        [NotNull] private readonly Logger _logger;
        [NotNull] private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public Trader(BinanceClient binanceClient, Logger logger, List<string> currencies)
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
            _timer.Start();
            await ExecuteSafe(ExecuteScheduledTasks);
        }

        public async void OnTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            await ExecuteSafe(ExecuteScheduledTasks);
        }

        private async void OnOrderChanged([NotNull] OrderOrTradeUpdatedMessage order)
        {
            await ExecuteSafe(async () =>
            {
                if (order.Status == OrderStatus.Filled)
                {
                    _logger.LogOrder("Completed", order, "ChangedEvent");

                    var hasOpenOrder = (await GetOpenOrders()).Any(o => o.Symbol == order.Symbol);
                    if (!hasOpenOrder)
                    {
                        var newOrder = await PlaceOppositeOrder(order);
                        _logger.LogOrder("Placed", newOrder, "ChangedEvent");
                    }
                    else
                    {
                        _logger.LogOrder("OrderAlreadyExists", order, "ChangedEvent");
                    }
                }
            });
        }

        private async Task ExecuteScheduledTasks()
        {
            await CheckCompletedOrders();
            await CheckOutdatedOrders();
            await InitOrdersUpdatesListening();
        }

        private async Task CheckOutdatedOrders()
        {
            var now = DateTime.Now;
            var outdatedOrders = (await GetOpenOrders())
                .Where(o => now - o.GetTime() > _maxIdlePeriod).ToList();

            foreach (var order in outdatedOrders)
            {
                if (order.Status == OrderStatus.New)
                {
                    await CancelOrder(order.Symbol, order.OrderId);
                    _logger.LogOrder("Canceled", order, "CheckOutdated");

                    var priceInfo = (await _binanceClient.GetPriceChange24H(order.Symbol)).First();

                    switch (order.Side)
                    {
                        case OrderSide.Buy:
                        {
                            var amount = order.Price * order.OrigQty;
                            var price = priceInfo.AskPrice;
                            var qty = Math.Floor(amount / price);

                            var newOrder = await PlaceOrder(OrderSide.Buy, order.Symbol, price, qty);
                            _logger.LogOrder("Placed", newOrder, "CheckOutdated");
                            break;
                        }
                        case OrderSide.Sell:
                        {
                            var newOrder = await PlaceOrder(OrderSide.Sell, order.Symbol, priceInfo.BidPrice,
                                order.OrigQty);
                            _logger.LogOrder("Placed", newOrder, "CheckOutdated");
                            break;
                        }
                    }
                }
            }

            _logger.Log("CheckOutdatedOrders");
        }

        private Task<IEnumerable<Order>> GetOpenOrders()
        {
            return _binanceClient.GetCurrentOpenOrders();
        }

        private async Task CheckCompletedOrders()
        {
            foreach (var currency in _currencies)
            {
                var lastOrder = await GetLastOrder(currency);
                if (lastOrder.Status == OrderStatus.Filled)
                {
                    var newOrder = await PlaceOppositeOrder(lastOrder);
                    _logger.LogOrder("Placed", newOrder, "CheckCompleted");
                }
            }

            _logger.Log("CheckCompletedOrders");
        }

        private async Task<IOrder> PlaceOppositeOrder(IOrder order)
        {
            switch (order.Side)
            {
                case OrderSide.Sell:
                {
                    var amount = order.Price * order.OrigQty;
                    var price = (order.Price - order.Price.Percents(ProfitRatio)).Round();
                    var qty = Math.Floor(amount / price);

                    if (amount > MinQuoteAmount && qty > 0)
                    {
                        return await PlaceOrder(OrderSide.Buy, order.Symbol, price, qty);
                    }

                    _logger.Log($"Insufficient balance {order.Symbol}");

                    break;
                }
                case OrderSide.Buy:
                {
                    var price = (order.Price + order.Price.Percents(ProfitRatio)).Round();

                    return await PlaceOrder(OrderSide.Sell, order.Symbol, price, order.OrigQty);
                }
            }

            return null;
        }

        private async Task InitOrdersUpdatesListening()
        {
            if (_listenKey != null)
            {
                await _binanceClient.CloseUserStream(_listenKey);
            }

            _listenKey = _binanceClient.ListenUserDataEndpoint(
                _ => { },
                OnOrderChanged,
                OnOrderChanged);

            _logger.Log("InitOrdersUpdatesListening");
        }

        private async Task<Order> GetLastOrder(string currency)
        {
            var lastOrder = (await _binanceClient.GetAllOrders(currency, null, 1)).First();
            return lastOrder;
        }

        //private async Task<decimal> GetFreeQuoteAmount()
        //{
        //    var freeQuoteAmount = (await _binanceClient.GetAccountInfo()).Balances
        //        .First(b => b.Asset == QuoteAsset).Free;

        //    return freeQuoteAmount;
        //}

        private async Task<NewOrder> PlaceOrder(OrderSide side, string symbol, decimal price, decimal qty)
        {
            var newOrder = await _binanceClient.PostNewOrder(
                symbol,
                qty,
                price,
                side);

            return newOrder;
        }

        private async Task<CanceledOrder> CancelOrder(string symbol, long orderId)
        {
            var canceledOrder = await _binanceClient.CancelOrder(
                symbol,
                orderId
            );

            return canceledOrder;
        }

        private async Task ExecuteSafe(Func<Task> action)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                await action();
            }
            catch (BinanceApiException ex)
            {
                _logger.Log(ex);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}