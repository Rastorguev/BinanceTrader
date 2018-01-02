using System;
using System.Linq;
using BinanceTrader.Api;
using BinanceTrader.Entities;

namespace BinanceTrader
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //Trade();

            var api = new BinanceApi(new BinanceKeyProvider("d:/Keys.config"));
            var candles = new Candles(api.GetCandles("XVG", "ETH", "1m").Result.OrderBy(c => c.OpenTime).ToList());
            var candle = candles.Last();
            var ot = candle.OpenTime;
            var ct = candle.CloseTime;

            var av7 = decimal.Round(CalculateAveragePrice(candles, 7), 8);
            var av25 = decimal.Round(CalculateAveragePrice(candles, 25), 8);

            var s = "";

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

        private static decimal CalculateAveragePrice(Candles candles, int n)
        {
            var range = candles.GetRange(candles.Count - n, n);

            var av = range.Average(c => c.ClosePrice);

            return av;
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