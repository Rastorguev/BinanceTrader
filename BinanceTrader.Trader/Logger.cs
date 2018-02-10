using System;
using Binance.API.Csharp.Client.Models.Account;
using Binance.API.Csharp.Client.Models.Enums;
using BinanceTrader.Tools;

namespace BinanceTrader
{
    public class Logger
    {
        public void LogOrderPlaced(OrderSide side, string symbol, decimal price, decimal qty, bool force)
        {
            LogTime();
            Console.WriteLine("Placed");
            Console.WriteLine($"Symbol:\t\t {symbol}");
            Console.WriteLine($"Side:\t\t {side}");
            Console.WriteLine($"Price:\t\t {price.Round()}");
            Console.WriteLine($"Qty:\t\t {qty.Round()}");

            if (force)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FORCE");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        public void LogOrderCompleted(OrderSide side, string symbol, OrderStatus status, decimal price, decimal qty)
        {
            LogTime();
            Console.WriteLine("Completed");
            Console.WriteLine($"Symbol:\t\t {symbol}");
            Console.WriteLine($"Side:\t\t {side}");
            Console.WriteLine($"Status:\t\t {status}");
            Console.WriteLine($"Price:\t\t {price.Round()}");
            Console.WriteLine($"Qty:\t\t {qty.Round()}");
            Console.WriteLine();
        }

        public void LogOrderCanceled(CanceledOrder order)
        {
            LogTime();
            Console.WriteLine("Canceled");
            Console.WriteLine($"{order.Symbol}");
            Console.WriteLine();
        }

        public void Log(Exception ex)
        {
            LogTime();
            Console.WriteLine(ex);
            Console.WriteLine();
        }

        public void Log(string message)
        {
            LogTime();
            Console.WriteLine(message);
            Console.WriteLine();
        }

        private static void LogTime()
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString());
        }
    }
}