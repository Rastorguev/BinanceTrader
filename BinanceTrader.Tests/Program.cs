using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using BinanceTrader.Strategies;
using BinanceTrader.Tools;
using BinanceTrader.TradeSessions;

namespace BinanceTrader
{
    internal class Program
    {
        private static void Main()
        {
            ServicePointManager.DefaultConnectionLimit = 15;

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var keyProvider = new BinanceKeyProvider(@"D:/Keys.config");
            var keys = keyProvider.GetKeys().NotNull();
            var apiClient = new ApiClient(keys.ApiKey, keys.SecretKey);
            var binanceClient = new BinanceClient(apiClient);

            var tests = new StrategiesTests(binanceClient);

            ITradeSession SessionProvider() =>
                new StrategyTradeSession(
                    new TradeSessionConfig(
                        initialQuoteAmount: 1,
                        initialPrice: 0,
                        fee: 0.1m,
                        minQuoteAmount: 0.01m,
                        minProfitRatio: 0.5m,
                        maxIdleHours: 8),
                    new EMACrossingTradeStrategy(7, 25)
                );

            tests.Run(SessionProvider);

            PreventAppClose();
        }

        private static void PreventAppClose()
        {
            Task.Delay(-1).Wait();
        }
    }
}