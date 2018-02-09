using System;
using BinanceTrader.Api;

namespace BinanceTrader
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var trader=new Trader();
            trader.Start();

            //var keyProvider = new MockKeyProvider();
            //var test = new StrategiesTests(new BinanceApi(keyProvider));
            //test.CompareStrategies();

            PreventAppClose();
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