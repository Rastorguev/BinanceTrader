using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Account;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.WebSocket;
using BinanceTrader.Api;
using BinanceTrader.Tools;
using JetBrains.Annotations;

namespace BinanceTrader
{
    public class Trader
    {
        [NotNull] private readonly Timer _timer;
        [NotNull] private readonly BinanceClient _binanceClient;
        [CanBeNull] private string _listenKey;

        private readonly TimeSpan _maxIdlePeriod = TimeSpan.FromMinutes(0.1);

        public Trader()
        {
            var keyProvider = new BinanceKeyProvider(@"D:/Keys.config");
            var keys = keyProvider.GetKeys().NotNull();
            var apiClient = new ApiClient(keys.ApiKey, keys.SecretKey);
            _binanceClient = new BinanceClient(apiClient);

            _timer = new Timer {Interval = _maxIdlePeriod.TotalMilliseconds, AutoReset = true};
            _timer.Elapsed += OnTimerElapsed;
        }

        public async void Start()
        {
            //_timer.Start();

            await StartTradesListening();
        }

        private async Task StartTradesListening()
        {
            try
            {
                if (_listenKey != null)
                {
                    await _binanceClient.CloseUserStream(_listenKey);
                }

                _listenKey = _binanceClient.ListenUserDataEndpoint(_ => { }, _ => { }, OrderHandler);
            }
            catch (BinanceApiException e)
            {
                Console.WriteLine(e);
            }
        }

        public async void OnTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            await StartTradesListening();
            //var now = DateTime.Now;
            //var outdatedOrders = (await _binanceClient.GetCurrentOpenOrders())
            //    .Where(o => now - o.GetTime() > _maxIdlePeriod);

            //foreach (var order in outdatedOrders)
            //{
            //    await ForceAction(order);
            //}
        }

        private void OrderHandler(OrderOrTradeUpdatedMessage message)
        {
        }

        private async Task ForceAction(Order order)
        {
            await _binanceClient.CancelOrder(order.Symbol, order.OrderId);
            var statistic = (await _binanceClient.GetPriceChange24H(order.Symbol)).First();

            if (order.Status == OrderStatus.New)
            {
                switch (order.Side)
                {
                    case OrderSide.Sell:
                        await _binanceClient.PostNewOrder(
                            order.Symbol,
                            statistic.BidPrice,
                            order.OrigQty,
                            OrderSide.Sell);
                        break;
                    case OrderSide.Buy:
                        await _binanceClient.PostNewOrder(
                            order.Symbol,
                            statistic.AskPrice,
                            order.OrigQty,
                            OrderSide.Buy);
                        break;
                }
            }
        }
    }
}