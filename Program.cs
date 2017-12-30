using System;

namespace BinanceTrader
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Trade();

            //var cr = new BinanceKeyProvider();

            //cr.GetSecretKey();
            // var api = new BinanceApi();
            //var r= api.GetAccountInfo().Result;
            PreventAppClose();
        }

        public static void Trade()
        {
            var trader = new Trader();
            trader.Trade("ADA", "ETH", 1);
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