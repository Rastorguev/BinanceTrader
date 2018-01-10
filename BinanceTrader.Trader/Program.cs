using System;
using BinanceTrader.Api;

namespace BinanceTrader
{
    internal class Program
    {
        private const string KeysFilePath = "d:/Keys.config";

        private static void Main(string[] args)
        {
            //Trade();

            MonitorPrices();

            PreventAppClose();
        }

        public static void MonitorPrices()
        {
            var monitor = new PriceMonitor(new BinanceApi(new BinanceKeyProvider(KeysFilePath)));
            monitor.Start();
        }

        //public static void Trade()
        //{
        //    var trader = new RabbitTrader(
        //        new BinanceApi(new BinanceKeyProvider(KeysFilePath)),
        //        "TNB",
        //        "ETH");
        //    trader.Start();
        //}

        private static void PreventAppClose()
        {
            while (true)
            {
                Console.ReadKey();
            }
        }

        private void ApiTest()
        {
            //var api = new BinanceApi(new BinanceKeyProvider("d:/Keys.config"));
            //var candles = new CandlesChart(api.GetCandles("XVG", "ETH", "1m").Result.OrderBy(c => c.OpenTime).ToList());
            //var candle = candles.Last();
            //var ot = candle.OpenTime;
            //var ct = candle.CloseTime;

            //var av7 = decimal.Round(CalculateAveragePrice(candles, 7), 8);
            //var av25 = decimal.Round(CalculateAveragePrice(candles, 25), 8);

            // var s = "";

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
        }
    }
}