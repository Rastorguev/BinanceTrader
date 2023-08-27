using System.Globalization;
using System.Net;
using BinanceTrader.Core;
using BinanceTrader.Tools;

namespace BinanceTrader.Cli;

internal class Program
{
    private static void Main(string[] args)
    {
        ServicePointManager.DefaultConnectionLimit = 10;

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        try
        {
            var traders = new List<string>
            {
                "Rambler",
                "Google"
            };

            TradeStarter.Start(traders, traderName => new Logger(traderName));
        }
        catch (Exception ex)
        {
            new Logger(string.Empty).LogException(ex);
        }

        PreventAppClose();
    }

    private static void PreventAppClose()
    {
        Task.Delay(-1).Wait();
    }
}