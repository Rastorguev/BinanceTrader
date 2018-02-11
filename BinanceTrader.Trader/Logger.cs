using System;
using Binance.API.Csharp.Client.Models.WebSocket;
using BinanceTrader.Tools;

namespace BinanceTrader
{
    public class Logger
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