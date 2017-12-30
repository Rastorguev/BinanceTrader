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
            //var order = api.CreateTestOrder().Result;

            PreventAppClose();
        }

        public static void Trade()
        {
            var trader = new Trader(new BinanceApi(new BinanceKeyProvider("d:/Keys.config")));
            trader.Trade("TRX", "ETH", 1);
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