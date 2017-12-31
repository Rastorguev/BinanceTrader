using System;
using BinanceTrader.Api;

namespace BinanceTrader
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Trade();

            //var api = new BinanceApi(new BinanceKeyProvider("d:/Keys.config"));

            //var info = api.GetAccountInfo().Result;

            //var trx = info.Balances.GetBalanceFor("TRX");
            //var eth = info.Balances.GetBalanceFor("ETH");

            //var baseCurrency = "TRX";
            //var quoteCurrency = "ETH";

            //var prices = api.GetPrices().Result;
            //var priceTicker = new PriceTicker(prices);
            ////var price = priceTicker.GetPrice(ApiUtils.CreateCurrencySymbol(baseCurrency, quoteCurrency)).Price;
            //var price = 0.00001m;

            //var q = 0.011m / price;
            //var quantity = Math.Ceiling(q);

            //var orderConfig = new OrderConfig
            //{
            //    BaseCurrency = baseCurrency,
            //    QuoteCurrency = quoteCurrency,
            //    Price = price,
            //    Quantity = quantity,
            //    TimeInForce = TimeInForceType.IOC,
            //    Side = OrderSide.Buy,
            //    Type = OrderType.Limit
            //};

            //var result = api.CreateOrder(orderConfig).Result;

            //Console.WriteLine($"{result}");

            PreventAppClose();
        }

        public static void Trade()
        {
            var trader = new Trader(
                new BinanceApi(new BinanceKeyProvider("d:/Keys.config")),
                "TRX",
                "ETH");
            trader.Trade();
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