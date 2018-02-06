using System;
using BinanceTrader.Core.Entities.Enums;

namespace BinanceTrader
{
    public class Loger
    {
        private const string CurrencyFormat = "0.00000000";

        private readonly string _quoteCurrency;
        private readonly string _baseCurrency;

        public Loger(
            string baseCurrency,
            string quoteCurrency)
        {
            _quoteCurrency = quoteCurrency;
            _baseCurrency = baseCurrency;
        }

        public void LogOrderPlaced(OrderSide side, OrderStatus status, decimal price)
        {
            Console.WriteLine("Order Placed");
            Console.WriteLine($"Side:\t\t {side}");
            Console.WriteLine($"Status:\t\t {status}");
            Console.WriteLine($"Time:\t\t {DateTime.Now.ToLongTimeString()}");
            Console.WriteLine($"Price:\t\t {price.ToString(CurrencyFormat)} {_quoteCurrency}");
            Console.WriteLine();
        }

        public void LogOrderComplited(OrderSide side, OrderStatus status, decimal price, decimal baseAmount,
            decimal quoteAmount, decimal totalAmount, decimal profit)
        {
            Console.WriteLine("Order Complited");
            Console.WriteLine($"Side:\t\t {side}");
            Console.WriteLine($"Status:\t\t {status}");
            Console.WriteLine($"Time:\t\t {DateTime.Now.ToLongTimeString()}");
            Console.WriteLine($"Price:\t\t {price.ToString(CurrencyFormat)} {_quoteCurrency}");
            Console.WriteLine($"{_baseCurrency} Amount:\t {baseAmount.ToString(CurrencyFormat)}");
            Console.WriteLine($"{_quoteCurrency} Amount:\t {quoteAmount.ToString(CurrencyFormat)}");
            Console.WriteLine($"Total Amount:\t {totalAmount.ToString(CurrencyFormat)}");
            Console.WriteLine($"Profit:\t\t {profit.ToString(CurrencyFormat)} %");
            Console.WriteLine();
        }

        //public void Log(TraderState state, decimal price)
        //{
        //    Console.WriteLine("Usnucced order");
        //    Console.WriteLine($"Time:\t\t {DateTime.Now.ToLongTimeString()}");
        //    Console.WriteLine($"Price:\t\t {price.ToString(CurrencyFormat)} {_quoteCurrency}");
        //    Console.WriteLine();
        //}
    }
}