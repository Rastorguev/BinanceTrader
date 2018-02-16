using System;
using Binance.API.Csharp.Client.Models.WebSocket;
using BinanceTrader.Tools;
using BinanceTrader.Trader;

namespace BinanceTrader.Cli
{
    public class Logger : ILogger
    {
        public void LogOrder(string orderEvent, IOrder order, string info = null)
        {
            LogTime();

            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine(orderEvent);

            Console.ResetColor();

            if (order != null)
            {
                Console.WriteLine($"Symbol:\t\t {order.Symbol}");
                Console.WriteLine($"Side:\t\t {order.Side}");
                Console.WriteLine($"Status:\t\t {order.Status}");
                Console.WriteLine($"Price:\t\t {order.Price.Round()}");
                Console.WriteLine($"Qty:\t\t {order.OrigQty.Round()}");
            }

            if (info != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(info);
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        public void Log(Exception ex)
        {
            LogTime();

            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(ex);
            Console.WriteLine();

            Console.ResetColor();
        }

        public void Log(string message)
        {
            LogTime();
            Console.WriteLine(message);
            Console.WriteLine();
        }

        public void LogImportant(string message)
        {
            LogTime();

            Console.ForegroundColor = ConsoleColor.Magenta;

            Console.WriteLine(message);
            Console.WriteLine();

            Console.ResetColor();
        }

        public void LogTitle(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"{title.ToUpper()}");
            Console.WriteLine();

            Console.ResetColor();
        }

        public void LogSeparator()
        {
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine();
        }

        private static void LogTime(DateTime? time = null)
        {
            var timeToLog = time ?? DateTime.Now;

            Console.WriteLine(timeToLog);
        }
    }
}