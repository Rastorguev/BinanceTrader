using Binance.API.Csharp.Client.Models.WebSocket;
using BinanceTrader.Tools;
using BinanceTrader.Trader;

namespace BinanceTrader.Cli;

public class Logger : ILogger
{
    private readonly string _traderName;

    public Logger(string traderName)
    {
        _traderName = traderName;
    }

    public void LogOrderPlaced(IOrder order)
    {
        LogOrder("Placed", order);
    }

    public void LogOrderCompleted(IOrder order)
    {
        LogOrder("Completed", order);
    }

    public void LogOrderCanceled(IOrder order)
    {
        LogOrder("Canceled", order);
    }

    public void LogOrderRequest(string eventName, OrderRequest orderRequest)
    {
        LogTime();
        LogTraderName();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(orderRequest.Symbol);
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(eventName);
        Console.ResetColor();

        Console.WriteLine($"Side:\t\t {orderRequest.Side}");
        Console.WriteLine($"Price:\t\t {orderRequest.Price.Round()}");
        Console.WriteLine($"Qty:\t\t {orderRequest.Qty.Round()}");

        Console.WriteLine();
    }

    public void LogException(Exception ex)
    {
        LogTime();
        LogTraderName();

        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine(ex);
        Console.WriteLine();

        Console.ResetColor();
    }

    public void LogMessage(string eventName, string message)
    {
        LogTime();
        LogTraderName();

        Console.WriteLine(eventName);
        Console.WriteLine(message);
        Console.WriteLine();
    }

    public void LogMessage(string eventName, Dictionary<string, string> properties)
    {
        LogTime();
        LogTraderName();

        Console.WriteLine(eventName);

        foreach (var pair in properties)
        {
            Console.WriteLine($"{pair.Key}: {pair.Value}");
        }

        Console.WriteLine();
    }

    private static void LogTime(DateTime? time = null)
    {
        var timeToLog = time ?? DateTime.Now;

        Console.WriteLine(timeToLog);
    }

    private void LogOrder(string eventName, IOrder order)
    {
        LogTime();
        LogTraderName();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(order.Symbol);
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(eventName);
        Console.ResetColor();

        Console.WriteLine($"Side:\t\t {order.Side}");
        Console.WriteLine($"Status:\t\t {order.Status}");
        Console.WriteLine($"Price:\t\t {order.Price.Round()}");
        Console.WriteLine($"Qty:\t\t {order.OrderQuantity.Round()}");

        Console.WriteLine();
    }

    private void LogTraderName()
    {
        Console.WriteLine($"Trader:\t {_traderName}");
    }
}