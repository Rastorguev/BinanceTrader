using System;
using BinanceTrader.Api;

namespace BinanceTrader
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var test = new StrategiesTests(new BinanceApi(new MockKeyProvider()));
            test.CompareStrategies();

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