using System;
using Binance.API.Csharp.Client.Models.WebSocket;
using BinanceTrader.Tools;
using BinanceTrader.Trader;

namespace BinanceTrader.Cli
{
    public class Logger : ILogger
    {
        public void LogOrder(string orderEvent, IOrder order)
        {
            LogTime();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(order.Symbol);
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(orderEvent);
            Console.ResetColor();

            Console.WriteLine($"Side:\t\t {order.Side}");
            Console.WriteLine($"Status:\t\t {order.Status}");
            Console.WriteLine($"Price:\t\t {order.Price.Round()}");
            Console.WriteLine($"Qty:\t\t {order.OrigQty.Round()}");

            Console.WriteLine();
        }

        public void LogException(Exception ex)
        {
            LogTime();

            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(ex);
            Console.WriteLine();

            Console.ResetColor();
        }

        public void LogMessage(string key, string message)
        {
            LogTime();
            Console.WriteLine(message);
            Console.WriteLine();
        }

        public void LogWarning(string key, string message)
        {
            LogTime();

            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine(message);
            Console.WriteLine();

            Console.ResetColor();
        }

        private static void LogTime(DateTime? time = null)
        {
            var timeToLog = time ?? DateTime.Now;

            Console.WriteLine(timeToLog);
        }
    }
}