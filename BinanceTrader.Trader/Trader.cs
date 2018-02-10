using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Account;
using Binance.API.Csharp.Client.Models.Enums;
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
        private const string QuoteAsset = "ETH";
        private const decimal MinQuoteAmount = 0.01m;

        [NotNull] private readonly BinanceClient _binanceClient;
        private readonly List<string> _currencies;
        [CanBeNull] private string _listenKey;
        [NotNull] private readonly Timer _timer;
        private readonly Logger _logger;

        public Trader(BinanceClient binanceClient, List<string> currencies)
        {
            _binanceClient = binanceClient;
            _currencies = currencies;

            _timer = new Timer
            {
                Interval = _scheduleInterval.TotalMilliseconds,
                AutoReset = true
            };
            _timer.Elapsed += OnTimerElapsed;
            _logger = new Logger();
        }

        public async void Start()
        {
            _timer.Start();

            await ExecuteScheduledTasks();
        }

        private async Task ExecuteScheduledTasks()
        {
            try
            {
                await InitOrdersUpdatesListening();
                await CheckOutdatedOrders();
                await CheckCompletedOrders();
            }
            catch (BinanceApiException ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task CheckOutdatedOrders()
        {
            var now = DateTime.Now;
            var outdatedOrders = (await _binanceClient.GetCurrentOpenOrders())
                .Where(o => now - o.GetTime() > _maxIdlePeriod).ToList();

            foreach (var order in outdatedOrders)
            {
                await CancelOrder(order.Symbol, order.OrderId);
                var priceInfo = (await _binanceClient.GetPriceChange24H(order.Symbol)).First();

                if (order.Status == OrderStatus.New)
                {
                    switch (order.Side)
                    {
                        case OrderSide.Buy:
                        {
                            await PlaceOrder(OrderSide.Buy, order.Symbol, priceInfo.AskPrice, order.OrigQty, true);
                            break;
                        }
                        case OrderSide.Sell:
                        {
                            await PlaceOrder(OrderSide.Sell, order.Symbol, priceInfo.BidPrice, order.OrigQty, true);
                            break;
                        }
                    }
                }
            }

            _logger.Log("CheckOutdatedOrders");
        }

        private async Task CheckCompletedOrders()
        {
            foreach (var currency in _currencies)
            {
                var lastOrder = await GetLastOrder(currency);
                if (lastOrder.Status == OrderStatus.Filled)
                {
                    await PlaceOppositeOrder(lastOrder);
                }
            }

            _logger.Log("CheckCompletedOrders");
        }

        private async Task<Order> GetLastOrder(string currency)
        {
            var lastOrder = (await _binanceClient.GetAllOrders(currency, null, 1)).First();
            return lastOrder;
        }

        private async Task PlaceOppositeOrder(IOrder order)
        {
            try
            {
                var symbol = order.Symbol;
                var orderPrice = order.Price;
                var orderQty = order.OrigQty;

                switch (order.Side)
                {
                    case OrderSide.Sell:
                    {
                        var freeQuoteAmount = await GetFreeQuoteAmount();
                        var amount = orderPrice * orderQty;
                        amount = freeQuoteAmount - amount < MinQuoteAmount ? freeQuoteAmount : amount;
                        var price = (orderPrice - orderPrice.Percents(ProfitRatio)).Round();
                        var qty = Math.Floor(amount / price);

                        if (amount > MinQuoteAmount && qty > 0)
                        {
                            await PlaceOrder(OrderSide.Buy, symbol, price, qty);
                        }
                        else
                        {
                            _logger.Log($"Insufficient balance {symbol}");
                        }

                        break;
                    }
                    case OrderSide.Buy:
                    {
                        var price = (orderPrice + orderPrice.Percents(ProfitRatio)).Round();

                        await PlaceOrder(OrderSide.Sell, symbol, price, orderQty);
                        break;
                    }
                }
            }
            catch (BinanceApiException ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task<decimal> GetFreeQuoteAmount()
        {
            var freeQuoteAmount = (await _binanceClient.GetAccountInfo()).Balances
                .First(b => b.Asset == QuoteAsset).Free;

            return freeQuoteAmount;
        }

        private async Task InitOrdersUpdatesListening()
        {
            if (_listenKey != null)
            {
                await _binanceClient.CloseUserStream(_listenKey);
            }

            _listenKey = _binanceClient.ListenUserDataEndpoint(_ => { }, OnOrderChanged, OnOrderChanged);

            _logger.Log("InitOrdersUpdatesListening");
        }

        private async Task CheckConnection()
        {
            try
            {
                if (_listenKey != null)
                {
                    await _binanceClient.KeepAliveUserStream(_listenKey);
                    _logger.Log("CheckConnection");
                }
            }
            catch (BinanceApiException ex1)
            {
                _logger.Log(ex1);

                try
                {
                    await InitOrdersUpdatesListening();
                }
                catch (BinanceApiException ex2)
                {
                    _logger.Log(ex2);
                }
            }
        }

        private async Task<NewOrder> PlaceOrder(OrderSide side, string symbol, decimal price, decimal qty,
            bool force = false)
        {
            var newOrder = await _binanceClient.PostNewOrder(
                symbol,
                qty,
                price,
                side);

            _logger.LogOrderPlaced(side, symbol, price, qty, force);

            return newOrder;
        }

        private async Task<CanceledOrder> CancelOrder(string symbol, long orderId)
        {
            var canceledOrder = await _binanceClient.CancelOrder(
                symbol,
                orderId
            );

            _logger.LogOrderCanceled(canceledOrder);

            return canceledOrder;
        }

        public async void OnTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            await ExecuteScheduledTasks();
        }

        private async void OnOrderChanged([NotNull] OrderOrTradeUpdatedMessage order)
        {
            if (order.Status == OrderStatus.Filled)
            {
                _logger.LogOrderCompleted(order.Side, order.Symbol, order.Status, order.Price, order.OrigQty);

                await PlaceOppositeOrder(order);
            }
        }
    }
}