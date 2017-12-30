using System;

namespace BinanceTrader
{
    public class Loger
    {
        private const string CurrencyFormat = "0.00000000000";

        private readonly string _quoteCurrency;
        private readonly string _baseCurrency;

        public Loger(
            string baseCurrency,
            string quoteCurrency)
        {
            _quoteCurrency = quoteCurrency;
            _baseCurrency = baseCurrency;
        }

        public void Log(TraderState state, decimal price, decimal baseAmount, decimal quoteAmount, decimal profit)
        {
            Console.WriteLine($"State:\t\t {state}");
            Console.WriteLine($"Time:\t\t {DateTime.Now.ToLongTimeString()}");
            Console.WriteLine($"Price:\t\t {price.ToString(CurrencyFormat)} {_quoteCurrency}");
            Console.WriteLine($"{_baseCurrency} Amount:\t {baseAmount.ToString(CurrencyFormat)}");
            Console.WriteLine($"{_quoteCurrency} Amount:\t {quoteAmount.ToString(CurrencyFormat)}");
            Console.WriteLine($"Profit:\t\t {profit.ToString(CurrencyFormat)} %");
            Console.WriteLine();
        }
    }
}