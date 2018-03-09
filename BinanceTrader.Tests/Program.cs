using System.Globalization;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using BinanceTrader.Strategies;
using BinanceTrader.Tools;
using BinanceTrader.TradeSessions;

namespace BinanceTrader
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var keyProvider = new BinanceKeyProvider(@"D:/Keys.config");
            var keys = keyProvider.GetKeys().NotNull();
            var apiClient = new ApiClient(keys.ApiKey, keys.SecretKey);
            var binanceClient = new BinanceClient(apiClient);

            var tests = new StrategiesTests(binanceClient);

            //ITradeSession SessionProvider() =>
            //    new RabbitTradeSession(
            //        new TradeSessionConfig(
            //            initialQuoteAmount: 1,
            //            initialPrice: 0,
            //            fee: 0.1m,
            //            minQuoteAmount: 0.01m,
            //            minProfitRatio: 2m,
            //            maxIdleHours: 8));

            ITradeSession SessionProvider() =>
                new StrategyTradeSession(
                    new TradeSessionConfig(
                        initialQuoteAmount: 1,
                        initialPrice: 0,
                        fee: 0.1m,
                        minQuoteAmount: 0.01m,
                        minProfitRatio: 0.2m,
                        maxIdleHours: 8),
                    new MACDHistStrategy(12, 26, 9)
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