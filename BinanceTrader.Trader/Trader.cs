using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
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

            binanceClient.ListenUserDataEndpoint(
                accountUpdatedMessage =>
                {
                  
                },
                orderUpdatedMessage =>
                {
                  
                },
                orderUpdatedMessage =>
                {
                    var orders = binanceClient.GetCurrentOpenOrders("ETHUSDT").Result;
                });



        }


    }
}
