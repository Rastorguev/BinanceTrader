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
        private readonly TimeSpan _maxIdlePeriod = TimeSpan.FromMinutes(10);
        private const decimal ProfitRatio = 0.05m;
        private const string QuiteAsset = "ETH";
        //private const decimal MinQuoteAmount = 0.01m;

        //[NotNull] private Dictionary<string, decimal> _reserve = new Dictionary<string, decimal>();
        [NotNull] private readonly BinanceClient _binanceClient;
        [CanBeNull] private string _listenKey;
        [NotNull] private readonly Timer _timer;

        public Trader(BinanceClient binanceClient)
        {
            _binanceClient = binanceClient;

            _timer = new Timer
            {
                Interval = _maxIdlePeriod.TotalMilliseconds,
                AutoReset = true
            };
            _timer.Elapsed += OnTimerElapsed;
        }

        public async void Start()
        {
            _timer.Start();
            await InitOrdersUpdatesListening();

            //await ExecuteScheduledTasks();
        }

        private async Task ExecuteScheduledTasks()
        {
            try
            {
                await Ping();
                //await InitOrdersUpdatesListening();
                //await CheckOutdatedOrders();
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
                .Where(o => now - o.GetTime() > _maxIdlePeriod);

            foreach (var order in outdatedOrders)
            {
                await ForceTrade(order);
            }
        }

        private async Task PlaceOppositeOrder(OrderOrTradeUpdatedMessage order)
        {
            try
            {
                switch (order.Side)
                {
                    case OrderSide.Sell:
                    {
                        var price = (order.Price - order.Price.Percents(ProfitRatio)).Round();
                        var amount = order.Price * order.OrigQty;
                        var qty = Math.Floor(amount / price);

                        await PlaceOrder(OrderSide.Buy, order.Symbol, price, qty);
                        break;
                    }
                    case OrderSide.Buy:
                    {
                        var price = (order.Price + order.Price.Percents(ProfitRatio)).Round();

                        await PlaceOrder(OrderSide.Sell, order.Symbol, price, order.OrigQty);
                        break;
                    }
                }
            }
            catch (BinanceApiException ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task ForceTrade(Order order)
        {
            await _binanceClient.CancelOrder(order.Symbol, order.OrderId);
            var priceInfo = (await _binanceClient.GetPriceChange24H(order.Symbol)).First();

            if (order.Status == OrderStatus.New)
            {
                switch (order.Side)
                {
                    case OrderSide.Buy:
                    {
                        await PlaceOrder(OrderSide.Buy, order.Symbol, priceInfo.AskPrice, order.OrigQty);
                        break;
                    }
                    case OrderSide.Sell:
                    {
                        await PlaceOrder(OrderSide.Sell, order.Symbol, priceInfo.BidPrice, order.OrigQty);
                        break;
                    }
                }
            }
        }

        private async Task InitOrdersUpdatesListening()
        {
            if (_listenKey != null)
            {
                await _binanceClient.CloseUserStream(_listenKey);
            }

            _listenKey = _binanceClient.ListenUserDataEndpoint(_ => { }, OnOrderChanged, OnOrderChanged);

            Log("InitOrdersUpdatesListening");
        }

        private async Task Ping()
        {
            if (_listenKey != null)
            {
                try
                {
                    await _binanceClient.KeepAliveUserStream(_listenKey);
                    Log("Ping");
                }
                catch (BinanceApiException ex)
                {
                    Log(ex);
                    await InitOrdersUpdatesListening();
                }
            }
        }

        private async Task<NewOrder> PlaceOrder(OrderSide side, string symbol, decimal price, decimal qty)
        {
            var newOrder = await _binanceClient.PostNewOrder(
                symbol,
                qty,
                price,
                side);

            LogOrderPlaced(side, symbol, price, qty);

            return newOrder;
        }

        public async void OnTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            await ExecuteScheduledTasks();
        }

        private async void OnOrderChanged([NotNull] OrderOrTradeUpdatedMessage order)
        {
            if (order.Status == OrderStatus.Filled)
            {
                LogOrderCompleted(order.Side, order.Symbol, order.Status, order.Price, order.OrigQty);

                await PlaceOppositeOrder(order);
            }
        }

        public void LogOrderPlaced(OrderSide side, string symbol, decimal price, decimal qty)
        {
            Console.WriteLine("Placed");
            Console.WriteLine($"{symbol}");
            Console.WriteLine($"Side:\t\t {side}");
            Console.WriteLine($"Time:\t\t {DateTime.Now.ToLongTimeString()}");
            Console.WriteLine($"Price:\t\t {price.Round()}");
            Console.WriteLine($"Qty:\t\t {qty.Round()}");
            Console.WriteLine();
        }

        public void LogOrderCompleted(OrderSide side, string symbol, OrderStatus status, decimal price, decimal qty)
        {
            Console.WriteLine("Completed");
            Console.WriteLine($"{symbol}");
            Console.WriteLine($"Side:\t\t {side}");
            Console.WriteLine($"Status:\t\t {status}");
            Console.WriteLine($"Time:\t\t {DateTime.Now.ToLongTimeString()}");
            Console.WriteLine($"Price:\t\t {price.Round()}");
            Console.WriteLine($"Qty:\t\t {qty.Round()}");
            Console.WriteLine();
        }

        public void Log(Exception ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine();
        }

        public void Log(string message)
        {
            Console.WriteLine(message);
            Console.WriteLine();
        }
    }
}