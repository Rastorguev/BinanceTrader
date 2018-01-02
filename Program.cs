using System;
using System.Linq;
using BinanceTrader.Api;
using BinanceTrader.Entities;
using BinanceTrader.Entities.Enums;
using BinanceTrader.Utils;

namespace BinanceTrader
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Trade();

            var api = new BinanceApi(new BinanceKeyProvider("d:/Keys.config"));

            //var order = api.GetAllOrders("TRX", "ETH", 5).Result.GetOrder(4541287);


            //var api = new BinanceApi(new BinanceKeyProvider("d:/Keys.config"));

            //var info = api.GetAccountInfo().Result;

            //var trx = info.Balances.GetBalanceFor("TRX");
            //var eth = info.Balances.GetBalanceFor("ETH");

            //var baseCurrency = "IOTA";
            //var quoteCurrency = "ETH";

            //var prices = api.GetPrices().Result;
            ////var price = prices.PriceFor(ApiUtils.CreateCurrencySymbol(baseCurrency, quoteCurrency)).Price;
            //var price = 0.00001m;

            //var q = 0.011m / price;
            //var quantity = Math.Ceiling(q);

            //var orderConfig = new OrderConfig
            //{
            //    BaseAsset = baseCurrency,
            //    QuoteAsset = quoteCurrency,
            //    Price = price,
            //    Quantity = quantity,
            //    TimeInForce = TimeInForceType.GTC,
            //    Side = OrderSide.Buy,
            //    Type = OrderType.Limit
            //};

            //var order = api.MakeOrder(orderConfig).Result;
            //var r2 = api.CancelOrder(baseCurrency, quoteCurrency, order.OrderId).Result;



            PreventAppClose();
        }

        public static void Trade()
        {
            var trader = new Trader(
                new BinanceApi(new BinanceKeyProvider("d:/Keys.config")),
                "XVG",
                "ETH");
            trader.Start();
        }

        private static void PreventAppClose()
        {
            while (true)
            {
                Console.ReadKey();
            }
        }
    }
}