using System;
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
        private readonly TimeSpan _maxIdlePeriod = TimeSpan.FromMinutes(0.1);
        private const decimal ProfitRatio = 2;

        [NotNull] private readonly BinanceClient _binanceClient;
        [CanBeNull] private string _listenKey;
        [NotNull] private readonly Timer _timer;

        public Trader(BinanceClient binanceClient)
        {
            _binanceClient = binanceClient;

            _timer = new Timer {Interval = _maxIdlePeriod.TotalMilliseconds, AutoReset = true};
            _timer.Elapsed += OnTimerElapsed;
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
                        var price = order.Price - order.Price.Percents(ProfitRatio);
                        var amount = order.Price * order.OrigQty;
                        var qty = Math.Floor(amount / price);

                        await Buy(order.Symbol, price, qty);
                        break;
                    }
                    case OrderSide.Buy:
                    {
                        var price = order.Price + order.Price.Percents(ProfitRatio);

                        await Sell(order.Symbol, price, order.OrigQty);
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
            var statistic = (await _binanceClient.GetPriceChange24H(order.Symbol)).First();

            if (order.Status == OrderStatus.New)
            {
                switch (order.Side)
                {
                    case OrderSide.Buy:
                    {
                        await Buy(order.Symbol, statistic.AskPrice, order.OrigQty);
                        break;
                    }
                    case OrderSide.Sell:
                    {
                        await Sell(order.Symbol, statistic.BidPrice, order.OrigQty);
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

            _listenKey = _binanceClient.ListenUserDataEndpoint(_ => { }, _ => { }, OnOrderChanged);
        }

        private Task<NewOrder> Buy(string symbol, decimal price, decimal quantity)
        {
            return _binanceClient.PostNewOrder(
                symbol,
                price,
                quantity,
                OrderSide.Buy);
        }

        private Task<NewOrder> Sell(string symbol, decimal price, decimal quantity)
        {
            return _binanceClient.PostNewOrder(
                symbol,
                price,
                quantity,
                OrderSide.Sell);
        }

        public async void OnTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            await ExecuteScheduledTasks();
        }

        private async void OnOrderChanged([NotNull] OrderOrTradeUpdatedMessage order)
        {
            if (order.Status == OrderStatus.Filled)
            {
                await PlaceOppositeOrder(order);
            }
        }
    }
}