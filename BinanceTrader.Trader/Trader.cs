using System.Threading;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Enums;
using BinanceTrader.Api;
using BinanceTrader.Tools;

namespace BinanceTrader
{
    public class Trader
    {
        public void Trade()
        {
            var keyProvider = new BinanceKeyProvider(@"D:/Keys.config");
            var keys = keyProvider.GetKeys().NotNull();
            var apiClient = new ApiClient(keys.ApiKey, keys.SecretKey);
            var binanceClient = new BinanceClient(apiClient);

            var prices = binanceClient.GetAllPrices().Result;
            var info = binanceClient.GetAccountInfo().Result;
            var priceChangeInfo = binanceClient.GetPriceChange24H("TRXETH").Result;

            var id = Thread.CurrentThread.ManagedThreadId;


            var orders1 = binanceClient.GetCurrentOpenOrders().Result;

            var listenKey = binanceClient.ListenUserDataEndpoint(
                accountUpdatedMessage => { },
                orderUpdatedMessage => { },
                orderUpdatedMessage =>
                {
                    var orders = binanceClient.GetCurrentOpenOrders().Result;
                });

            var newOrder = binanceClient.PostNewOrder("ADAETH", 40, 90, OrderSide.SELL, OrderType.LIMIT, icebergQty:9).Result;
        }
    }
}