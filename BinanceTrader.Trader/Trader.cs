using System;
using System.Linq;
using System.Timers;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Enums;
using BinanceTrader.Api;
using BinanceTrader.Tools;

namespace BinanceTrader
{
    public class Trader
    {
        private readonly Timer _timer;
        private readonly BinanceClient _binanceClient;

        public Trader()
        {
            var keyProvider = new BinanceKeyProvider(@"D:/Keys.config");
            var keys = keyProvider.GetKeys().NotNull();
            var apiClient = new ApiClient(keys.ApiKey, keys.SecretKey);
            _binanceClient = new BinanceClient(apiClient, true);

            _timer = new Timer { Interval = TimeSpan.FromSeconds(1).TotalMilliseconds, AutoReset = true };
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        public void Trade()
        {
            //var prices = _binanceClient.GetAllPrices().Result;
            //var info = _binanceClient.GetAccountInfo().Result;
            //var priceChangeInfo = _binanceClient.GetPriceChange24H("TRXETH").Result;

            //var id = Thread.CurrentThread.ManagedThreadId;

            //var orders1 = _binanceClient.GetCurrentOpenOrders().Result;

            //var listenKey = _binanceClient.ListenUserDataEndpoint(
            //    accountUpdatedMessage => { },
            //    orderUpdatedMessage => { },
            //    orderUpdatedMessage =>
            //    {
            //        var orders = _binanceClient.GetCurrentOpenOrders().Result;
            //    });

            //var newOrder = _binanceClient.PostNewOrder("ADAETH", 10, 90, OrderSide.SELL).Result;
        }

        public void Start()
        {
        }

        public async void OnTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
           
            var dt = DateTime.Now.ToLocalTime();
            var t = await _binanceClient.GetServerTime();
            var time = DateTimeOffset.FromUnixTimeMilliseconds(t.ServerTime).LocalDateTime;
            var dt1 = DateTime.Now.ToLocalTime();


            var symbol = "ADAETH";
            var openOrders = await _binanceClient.GetCurrentOpenOrders();
            var trxOrders = openOrders.Where(o => o.Symbol == symbol);

            foreach (var order in trxOrders)
            {
                await _binanceClient.CancelOrder(symbol, order.OrderId);
            }

            var newOrder = _binanceClient.PostNewOrder(symbol, 1, 90, OrderSide.SELL).Result;

            //var priceChangeInfo = _binanceClient.GetPriceChange24H(symbol).Result;

            //Console.WriteLine(priceChangeInfo.First().AskPrice);
        }
    }
}