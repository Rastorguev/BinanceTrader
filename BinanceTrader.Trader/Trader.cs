using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Account;
using Binance.API.Csharp.Client.Models.Enums;
using BinanceTrader.Api;
using BinanceTrader.Tools;

namespace BinanceTrader
{
    public class Trader
    {
        private readonly Timer _timer;
        private readonly BinanceClient _binanceClient;
        private readonly TimeSpan _maxIdlePeriod = TimeSpan.FromHours(4);

        public Trader()
        {
            var keyProvider = new BinanceKeyProvider(@"D:/Keys.config");
            var keys = keyProvider.GetKeys().NotNull();
            var apiClient = new ApiClient(keys.ApiKey, keys.SecretKey);
            _binanceClient = new BinanceClient(apiClient);

            _timer = new Timer {Interval = TimeSpan.FromSeconds(1).TotalMilliseconds, AutoReset = true};
            //_timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        public async void Trade()
        {
            //var prices = _binanceClient.GetAllPrices().Result;
            //var info = _binanceClient.GetAccountInfo().Result;

            try
            {
                var priceChangeInfo = await _binanceClient.GetPriceChange24H("ADAETH");
                var orders1 = await _binanceClient.GetCurrentOpenOrders();
                //var newOrder = await _binanceClient.PostNewOrder("ADAETH", 10, 9, OrderSide.SELL);
            }
            catch (InvalidRequestException ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public void Start()
        {
        }

        public async void OnTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            var now = DateTime.Now;
            var outdatedOrders = (await _binanceClient.GetCurrentOpenOrders())
                .Where(o => now - o.GetTime() > _maxIdlePeriod);

            foreach (var order in outdatedOrders)
            {
                await ForceAction(order);
            }
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